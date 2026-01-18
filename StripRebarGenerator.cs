using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitAIAgent
{
    public class WallRebarGenerator
    {
        public void Generate(Document doc, List<Element> foundations, IsolatedRebarConfig config)
        {
            using (Transaction t = new Transaction(doc, "Generate Wall Rebar"))
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
            // Create stirrups perpendicular to the longitudinal bars
            // Stirrups are rectangular/U-shaped links distributed along the curve
            
            BoundingBoxXYZ bbox = host.get_BoundingBox(null);
            if (bbox == null) return;

            double height = bbox.Max.Z - bbox.Min.Z;
            double cover = 50.0 / 304.8; // 50mm
            double mmToFeet = 0.00328084;

            // For Wall foundations, stirrups are typically vertical U-shapes or closed rectangles
            // spanning the width and height of the foundation
            
            // We'll create a simple approach: place stirrups as closed rectangular loops
            // perpendicular to the curve at regular spacing
            
            XYZ p1 = curve.GetEndPoint(0);
            XYZ p2 = curve.GetEndPoint(1);
            XYZ dir = (p2 - p1).Normalize();
            XYZ normal = XYZ.BasisZ.CrossProduct(dir).Normalize(); // Perpendicular horizontal
            
            // Create a single stirrup at the start and distribute along curve
            // Stirrup corner points (simplified rectangular cross-section)
            double halfWidth = width / 2 - cover;
            double bottomZ = bbox.Min.Z + cover;
            double topZ = bbox.Max.Z - cover;
            
            // Define stirrup shape on the cross-section
            XYZ corner1 = p1 + normal * halfWidth + XYZ.BasisZ * (bottomZ - p1.Z);
            XYZ corner2 = p1 - normal * halfWidth + XYZ.BasisZ * (bottomZ - p1.Z);
            XYZ corner3 = p1 - normal * halfWidth + XYZ.BasisZ * (topZ - p1.Z);
            XYZ corner4 = p1 + normal * halfWidth + XYZ.BasisZ * (topZ - p1.Z);
            
            List<Curve> stirrupCurves = new List<Curve>
            {
                Line.CreateBound(corner1, corner2),
                Line.CreateBound(corner2, corner3),
                Line.CreateBound(corner3, corner4),
                Line.CreateBound(corner4, corner1)
            };
            
            try
            {
                Rebar stirrup = Rebar.CreateFromCurves(doc, RebarStyle.Standard, config.StirrupBarType, null, null,
                    host, dir, stirrupCurves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, false);
                
                if (stirrup != null)
                {
                    double curveLength = curve.Length - (2 * cover);
                    stirrup.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(config.StirrupSpacing * mmToFeet, curveLength, true, true, true);
                }
            }
            catch { }
        }
    }
}
