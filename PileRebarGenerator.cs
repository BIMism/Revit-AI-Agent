using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitAIAgent
{
    public class PileRebarGenerator
    {
        public void Generate(Document doc, List<Element> foundations, IsolatedRebarConfig config)
        {
            using (Transaction t = new Transaction(doc, "Generate Pile Rebar"))
            {
                t.Start();
                foreach (var pile in foundations)
                {
                    GenerateSingle(doc, pile, config);
                }
                t.Commit();
            }
        }

        private void GenerateSingle(Document doc, Element host, IsolatedRebarConfig config)
        {
            // Piles are vertical elements.
            BoundingBoxXYZ bbox = host.get_BoundingBox(null);
            if (bbox == null) return;

            XYZ center = (bbox.Min + bbox.Max) / 2;
            double zBottom = bbox.Min.Z;
            double zTop = bbox.Max.Z;

            // Dimensions
            double width = bbox.Max.X - bbox.Min.X;
            double depth = bbox.Max.Y - bbox.Min.Y;
            double diameter = width; // Assuming circular if close enough

            bool isCircular = Math.Abs(width - depth) < 0.01; // Simple check

            double cover = 0.164; // 50mm

            // 1. VERTICAL BARS (Cage)
            // Reuse B1 Type for Vertical
            if (config.BottomBarX != null)
            {
                // Create a single vertical curve
                XYZ p1 = new XYZ(center.X + (width/2 - cover), center.Y, zBottom + cover);
                XYZ p2 = new XYZ(center.X + (width/2 - cover), center.Y, zTop - cover);
                Line vertLine = Line.CreateBound(p1, p2);
                List<Curve> curves = new List<Curve> { vertLine };

                try 
                {
                    // For Circular piles, we use Radial distribution? 
                    // Or actually, CreateFromCurves with a Multi-Rebar set might be tricky for Circular.
                    // Easier method: Create Fixed Number parallel to a face, or manually place N bars rotated.
                    
                    // For V1 PROTOTYPE: Just placing a rectangular distribution
                     Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, config.BottomBarX, null, null,
                        host, XYZ.BasisY, curves, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                    
                    if (rebar != null)
                    {
                        // Fixed number: e.g. 6 bars
                        rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(6, width - 2*cover, true, true, true);
                    }
                }
                catch {}
            }

            // 2. TIES / SPIRALS
            // Reuse Stirrup Type
            if (config.StirrupsEnabled && config.StirrupBarType != null)
            {
                // Create shape 
                // Creating a spiral in API is handled by Rebar.CreateFromCurves with a specialized curve or RebarShape.
                // For V1: Simple Tie loops at spacing
            }
        }
    }
}
