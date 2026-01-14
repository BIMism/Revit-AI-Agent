using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitAIAgent
{
    public class IsolatedRebarConfig
    {
        // Bottom Bars
        public RebarBarType BottomBarX { get; set; }
        public RebarBarType BottomBarY { get; set; }
        public double SpacingBottomX { get; set; } // mm
        public double SpacingBottomY { get; set; } // mm
        public RebarHookType HookBottomX { get; set; }
        public RebarHookType HookBottomY { get; set; }

        // Top Bars
        public bool TopBarsEnabled { get; set; }
        public RebarBarType TopBarX { get; set; }
        public RebarBarType TopBarY { get; set; }
        public double SpacingTopX { get; set; } // mm
        public double SpacingTopY { get; set; } // mm
        public RebarHookType HookTopX { get; set; }
        public RebarHookType HookTopY { get; set; }

        // Dowels
        public bool DowelsEnabled { get; set; }
        public RebarBarType DowelBarType { get; set; }
        public RebarHookType DowelHookBase { get; set; }
        public int DowelCount { get; set; }
        public double DowelLength { get; set; } // mm

        // Stirrups
        public bool StirrupsEnabled { get; set; }
        public RebarBarType StirrupBarType { get; set; }
        public double StirrupSpacing { get; set; } // mm
    }

    public class IsolatedRebarGenerator
    {
        public void Generate(Document doc, List<Element> foundations, IsolatedRebarConfig config)
        {
            using (Transaction t = new Transaction(doc, "Generate Isolated Rebar"))
            {
                t.Start();

                foreach (var foundation in foundations)
                {
                    try
                    {
                        GenerateForSingle(doc, foundation, config);
                    }
                    catch (Exception ex)
                    {
                        // Log error for this specific foundation but continue
                    }
                }

                t.Commit();
            }
        }

        private void GenerateForSingle(Document doc, Element foundation, IsolatedRebarConfig config)
        {
            BoundingBoxXYZ bbox = foundation.get_BoundingBox(null);
            if (bbox == null) return;

            XYZ min = bbox.Min;
            XYZ max = bbox.Max;
            
            double coverDist = 0.05; // 50mm default cover (feet)
            double mmToFeet = 0.00328084;
            
            // BOTTOM BARS X (Longitudinal)
            double bottomZ = min.Z + coverDist;
            XYZ startX = new XYZ(min.X + coverDist, min.Y + coverDist, bottomZ);
            XYZ endX = new XYZ(max.X - coverDist, min.Y + coverDist, bottomZ);
            
            CreateRebarSet(doc, foundation, config.BottomBarX, config.HookBottomX, startX, endX, 
                XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.SpacingBottomX * mmToFeet);

            // BOTTOM BARS Y (Transversal)
            if (config.BottomBarX != null)
            {
                // In Revit 2022+, it is BarModelDiameter. In older versions BarDiameter. 
                // Using BarModelDiameter as it is more standard now.
                double barDiamX = config.BottomBarX.BarModelDiameter;
                double bottomZY = bottomZ + barDiamX; 
                
                XYZ startY = new XYZ(min.X + coverDist, min.Y + coverDist, bottomZY);
                XYZ endY = new XYZ(min.X + coverDist, max.Y - coverDist, bottomZY);

                CreateRebarSet(doc, foundation, config.BottomBarY, config.HookBottomY, startY, endY,
                    XYZ.BasisX, max.X - min.X - (2 * coverDist), config.SpacingBottomY * mmToFeet);
            }

            // TOP BARS
            if (config.TopBarsEnabled)
            {
                double topZ = max.Z - coverDist;

                // Top X
                XYZ startTopX = new XYZ(min.X + coverDist, min.Y + coverDist, topZ);
                XYZ endTopX = new XYZ(max.X - coverDist, min.Y + coverDist, topZ);
                CreateRebarSet(doc, foundation, config.TopBarX, config.HookTopX, startTopX, endTopX,
                    XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.SpacingTopX * mmToFeet);

                // Top Y (Under Top X)
                if (config.TopBarX != null)
                {
                    double barDiamTopX = config.TopBarX.BarModelDiameter;
                    double topZY = topZ - barDiamTopX;
                    XYZ startTopY = new XYZ(min.X + coverDist, min.Y + coverDist, topZY);
                    XYZ endTopY = new XYZ(min.X + coverDist, max.Y - coverDist, topZY);
                    CreateRebarSet(doc, foundation, config.TopBarY, config.HookTopY, startTopY, endTopY,
                        XYZ.BasisX, max.X - min.X - (2 * coverDist), config.SpacingTopY * mmToFeet);
                }
            }

            // DOWELS (Simple 4-bar placeholder)
            if (config.DowelsEnabled)
            {
                XYZ center = (min + max) / 2.0;
                double dowelBottomZ = min.Z + coverDist;
                double dowelLenFeet = config.DowelLength * mmToFeet;
                double offset = 1.0; // 1 ft offset from center constant for now
                
                List<XYZ> dowelPoints = new List<XYZ>
                {
                    new XYZ(center.X - offset, center.Y - offset, dowelBottomZ),
                    new XYZ(center.X + offset, center.Y - offset, dowelBottomZ),
                    new XYZ(center.X + offset, center.Y + offset, dowelBottomZ),
                    new XYZ(center.X - offset, center.Y + offset, dowelBottomZ)
                };

                foreach (var pt in dowelPoints)
                {
                    XYZ endPt = new XYZ(pt.X, pt.Y, pt.Z + dowelLenFeet);
                    Line curve = Line.CreateBound(pt, endPt);
                    List<Curve> curves = new List<Curve> { curve };
                    
                    try {
                        Rebar.CreateFromCurves(doc, RebarStyle.Standard, config.DowelBarType, 
                        config.DowelHookBase, null, foundation, XYZ.BasisY, curves, 
                        RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                    } catch {}
                }
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
                    host, distributionDir, curves, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);

                if (rebar != null && spacing > 0)
                {
                    rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(spacing, distributionLength, true, true, true);
                }
            }
            catch { }
        }
    }
}
