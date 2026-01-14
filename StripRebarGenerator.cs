using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitAIAgent
{
    public class StripRebarGenerator
    {
        public void Generate(Document doc, List<Element> foundations, IsolatedRebarConfig config)
        {
            using (Transaction t = new Transaction(doc, "Generate Strip Rebar"))
            {
                t.Start();
                foreach (var footing in foundations)
                {
                    GenerateSingle(doc, footing, config);
                }
                t.Commit();
            }
        }

        private void GenerateSingle(Document doc, Element host, IsolatedRebarConfig config)
        {
            // For Strip Footings (Wall Foundation), we rely on the LocationCurve
            LocationCurve locCurve = host.Location as LocationCurve;
            if (locCurve == null) return;

            Curve curve = locCurve.Curve;
            
            // Get Dimensions
            Parameter pWidth = host.LookupParameter("Width");
            double width = pWidth != null ? pWidth.AsDouble() : 2.0; // Default 2ft

            // 1. Longitudinal Bars (Along the curve)
            // We reuse B1 settings for Longitudinal
            if (config.BottomBarX != null)
            {
                CreateLongitudinalBars(doc, host, curve, width, config);
            }

            // 2. Stirrups (Perpendicular to curve)
            // We reuse Stirrup settings
            if (config.StirrupsEnabled && config.StirrupBarType != null)
            {
                CreateStirrups(doc, host, curve, width, config);
            }
        }

        private void CreateLongitudinalBars(Document doc, Element host, Curve curve, double width, IsolatedRebarConfig config)
        {
            // Create a set of bars distributed across the Width
            // Start Point: curve start
            // End Point: curve end
            // Distribution: Width
            
            // For simplicity in v1, we assume a linear strip footing and place bars parallel to curve
            XYZ p1 = curve.GetEndPoint(0);
            XYZ p2 = curve.GetEndPoint(1);
            XYZ dir = (p2 - p1).Normalize();
            XYZ normal = XYZ.BasisZ.CrossProduct(dir); // Width direction
            
            double cover = 0.164; // 50mm
            
            // Rebar Shape: Straight (or with hooks)
            // We use standard CreateFromCurves
            
            // NOTE: Wall Foundations are tricky with CreateFromCurves because 'Curve' must be inside.
            // A safer bet is to use the host boundary box for placement if simple LocationCurve fails.
            
            // Let's try placement centered on the curve
            Line barLine = Line.CreateBound(p1, p2);
            List<Curve> curves = new List<Curve> { barLine };
            
            try
            {
                Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, config.BottomBarX, config.HookBottomX, config.HookBottomX,
                    host, normal, curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                
                if (rebar != null)
                {
                    double distLength = width - (2 * cover);
                    rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(config.SpacingBottomX * 0.00328, distLength, true, true, true);
                    // Move to bottom face
                    // (Logic to move bar to bottom Z would go here, often ElementTransformUtils.Move)
                }
            }
            catch {}
        }

        private void CreateStirrups(Document doc, Element host, Curve curve, double width, IsolatedRebarConfig config)
        {
            // Similar logic but Shape is Stirrup (O-shape or U-shape) and Distribution is along the Curve
        }
    }
}
