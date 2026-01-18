using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitAIAgent
{
    public class SlabRebarGenerator
    {
        public void Generate(Document doc, List<Element> foundations, IsolatedRebarConfig config)
        {
            using (Transaction t = new Transaction(doc, "Generate Slab Rebar"))
            {
                t.Start();
                foreach (var slab in foundations)
                {
                    GenerateSingle(doc, slab, config);
                }
                t.Commit();
            }
        }

        private void GenerateSingle(Document doc, Element host, IsolatedRebarConfig config)
        {
            // Slabs/Rafts are large flat elements
            // Similar to Isolated but optimized for large areas
            
            BoundingBoxXYZ bbox = host.get_BoundingBox(null);
            if (bbox == null) return;

            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            double coverDist = 50.0 / 304.8; // 50mm
            double mmToFeet = 0.00328084;

            // BOTTOM MESH (Similar to Isolated B1/B2)
            if (config.BottomBarX != null && config.BottomBarY != null)
            {
                double bottomZ = min.Z + coverDist;
                
                // X Direction
                XYZ startX = new XYZ(min.X + coverDist, min.Y + coverDist, bottomZ);
                XYZ endX = new XYZ(max.X - coverDist, min.Y + coverDist, bottomZ);
                CreateRebarSet(doc, host, config.BottomBarX, config.HookBottomX, startX, endX,
                    XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.SpacingBottomX * mmToFeet);

                // Y Direction
                double barDiamX = config.BottomBarX.BarModelDiameter;
                double bottomZY = bottomZ + barDiamX;
                XYZ startY = new XYZ(min.X + coverDist, min.Y + coverDist, bottomZY);
                XYZ endY = new XYZ(min.X + coverDist, max.Y - coverDist, bottomZY);
                CreateRebarSet(doc, host, config.BottomBarY, config.HookBottomY, startY, endY,
                    XYZ.BasisX, max.X - min.X - (2 * coverDist), config.SpacingBottomY * mmToFeet);
            }

            // TOP MESH (if enabled)
            if (config.TopBarsEnabled && config.TopBarX != null && config.TopBarY != null)
            {
                double topZ = max.Z - coverDist;
                
                // X Direction
                XYZ startTopX = new XYZ(min.X + coverDist, min.Y + coverDist, topZ);
                XYZ endTopX = new XYZ(max.X - coverDist, min.Y + coverDist, topZ);
                CreateRebarSet(doc, host, config.TopBarX, config.HookTopX, startTopX, endTopX,
                    XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.SpacingTopX * mmToFeet);

                // Y Direction
                double barDiamTopX = config.TopBarX.BarModelDiameter;
                double topZY = topZ - barDiamTopX;
                XYZ startTopY = new XYZ(min.X + coverDist, min.Y + coverDist, topZY);
                XYZ endTopY = new XYZ(min.X + coverDist, max.Y - coverDist, topZY);
                CreateRebarSet(doc, host, config.TopBarY, config.HookTopY, startTopY, endTopY,
                    XYZ.BasisX, max.X - min.X - (2 * coverDist), config.SpacingTopY * mmToFeet);
            }
        }

        private void CreateRebarSet(Document doc, Element host, RebarBarType barType, RebarHookType hookType,
            XYZ start, XYZ end, XYZ distributionDir, double distributionLength, double spacing)
        {
            if (barType == null) return;

            Line curve = Line.CreateBound(start, end);
            List<Curve> curves = new List<Curve> { curve };

            try
            {
                Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType, hookType,
                    host, distributionDir, curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);

                if (rebar != null && spacing > 0)
                    rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(spacing, distributionLength, true, true, true);
            }
            catch { }
        }
    }
}
