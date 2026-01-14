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
        public double? OverrideHookLenBottomX { get; set; } // Null if no override
        public double? OverrideHookLenBottomY { get; set; }
        
        public bool AddB1Enabled { get; set; }
        public RebarBarType AddB1Type { get; set; }
        public int AddB1Count { get; set; }

        public bool AddB2Enabled { get; set; }
        public RebarBarType AddB2Type { get; set; }
        public int AddB2Count { get; set; }

        // Top Bars
        public bool TopBarsEnabled { get; set; }
        public RebarBarType TopBarX { get; set; }
        public RebarBarType TopBarY { get; set; }
        public double SpacingTopX { get; set; } // mm
        public double SpacingTopY { get; set; } // mm
        public RebarHookType HookTopX { get; set; }
        public RebarHookType HookTopY { get; set; }
        public double? OverrideHookLenTopX { get; set; }
        public double? OverrideHookLenTopY { get; set; }

        public bool AddT1Enabled { get; set; }
        public RebarBarType AddT1Type { get; set; }
        public int AddT1Count { get; set; }

        public bool AddT2Enabled { get; set; }
        public RebarBarType AddT2Type { get; set; }
        public int AddT2Count { get; set; }

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
                    catch
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
            
            // ... (rest of method)

            XYZ min = bbox.Min;
            XYZ max = bbox.Max;
            
            double coverDist = 0.05; // 50mm default cover (feet)
            double mmToFeet = 0.00328084;
            
            // BOTTOM BARS X (Longitudinal)
            double bottomZ = min.Z + coverDist;
            XYZ startX = new XYZ(min.X + coverDist, min.Y + coverDist, bottomZ);
            XYZ endX = new XYZ(max.X - coverDist, min.Y + coverDist, bottomZ);
            
            CreateRebarSet(doc, foundation, config.BottomBarX, config.HookBottomX, startX, endX, 
                XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.SpacingBottomX * mmToFeet, config.OverrideHookLenBottomX * mmToFeet);

            if (config.AddB1Enabled && config.AddB1Count > 0)
            {
                // Create additional bars distributed same as B1
                // Just use fixed count instead of spacing
                CreateFixedRebarSet(doc, foundation, config.AddB1Type, config.HookBottomX, startX, endX, 
                    XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.AddB1Count); 
            }

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
                    XYZ.BasisX, max.X - min.X - (2 * coverDist), config.SpacingBottomY * mmToFeet, config.OverrideHookLenBottomY * mmToFeet);  // Same as B1
                    
                if (config.AddB2Enabled && config.AddB2Count > 0)
                {
                   CreateFixedRebarSet(doc, foundation, config.AddB2Type, config.HookBottomY, startY, endY,
                    XYZ.BasisX, max.X - min.X - (2 * coverDist), config.AddB2Count);
                }
            }

            // TOP BARS
            if (config.TopBarsEnabled)
            {
                double topZ = max.Z - coverDist;

                // Top X
                XYZ startTopX = new XYZ(min.X + coverDist, min.Y + coverDist, topZ);
                XYZ endTopX = new XYZ(max.X - coverDist, min.Y + coverDist, topZ);
                CreateRebarSet(doc, foundation, config.TopBarX, config.HookTopX, startTopX, endTopX,
                    XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.SpacingTopX * mmToFeet, config.OverrideHookLenTopX * mmToFeet,
                    RebarHookOrientation.Right, RebarHookOrientation.Left);

                if (config.AddT1Enabled && config.AddT1Count > 0)
                {
                    CreateFixedRebarSet(doc, foundation, config.AddT1Type, config.HookTopX, startTopX, endTopX,
                    XYZ.BasisY, max.Y - min.Y - (2 * coverDist), config.AddT1Count,
                    RebarHookOrientation.Right, RebarHookOrientation.Left);
                }

                // Top Y (Under Top X)
                if (config.TopBarX != null)
                {
                    double barDiamTopX = config.TopBarX.BarModelDiameter;
                    double topZY = topZ - barDiamTopX;
                    XYZ startTopY = new XYZ(min.X + coverDist, min.Y + coverDist, topZY);
                    XYZ endTopY = new XYZ(min.X + coverDist, max.Y - coverDist, topZY);
                    CreateRebarSet(doc, foundation, config.TopBarY, config.HookTopY, startTopY, endTopY,
                        XYZ.BasisX, max.X - min.X - (2 * coverDist), config.SpacingTopY * mmToFeet, config.OverrideHookLenTopY * mmToFeet);  // Same as T1

                    if (config.AddT2Enabled && config.AddT2Count > 0)
                    {
                        CreateFixedRebarSet(doc, foundation, config.AddT2Type, config.HookTopY, startTopY, endTopY,
                        XYZ.BasisX, max.X - min.X - (2 * coverDist), config.AddT2Count);
                    }
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
                        RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);  // Right = hook at bottom points up
                    } catch {}
                }
            }
        }

        private void CreateRebarSet(Document doc, Element host, RebarBarType barType, RebarHookType hookType, 
            XYZ start, XYZ end, XYZ distributionDir, double distributionLength, double spacing, double? overrideHookLen = null,
            RebarHookOrientation hookOrientStart = RebarHookOrientation.Left, 
            RebarHookOrientation hookOrientEnd = RebarHookOrientation.Right)
        {
            if (barType == null) return;
            
            Line curve = Line.CreateBound(start, end);
            List<Curve> curves = new List<Curve> { curve };

            try
            {
                Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType, hookType, 
                    host, distributionDir, curves, hookOrientStart, hookOrientEnd, true, true);

                if (rebar != null)
                {
                    if (spacing > 0)
                        rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(spacing, distributionLength, true, true, true);
                    
                    if (overrideHookLen.HasValue && hookType != null)
                    {
                        // Attempt to override hook lengths
                        // "Override Hook Lengths" is the checkbox parameter
                        Parameter pOverride = rebar.LookupParameter("Override Hook Lengths");
                        if (pOverride != null && !pOverride.IsReadOnly) pOverride.Set(1);

                        // "Hook Length Start" and "Hook Length End"
                        Parameter pStart = rebar.LookupParameter("Hook Length Start");
                        Parameter pEnd = rebar.LookupParameter("Hook Length End");
                        
                        if (pStart != null && !pStart.IsReadOnly) pStart.Set(overrideHookLen.Value);
                        if (pEnd != null && !pEnd.IsReadOnly) pEnd.Set(overrideHookLen.Value);
                    }
                }
            }
            catch { }
        }

        private void CreateFixedRebarSet(Document doc, Element host, RebarBarType barType, RebarHookType hookType, 
            XYZ start, XYZ end, XYZ distributionDir, double distributionLength, int count,
            RebarHookOrientation hookOrientStart = RebarHookOrientation.Left, 
            RebarHookOrientation hookOrientEnd = RebarHookOrientation.Right)
        {
            if (barType == null || count < 2) return;
            
            Line curve = Line.CreateBound(start, end);
            List<Curve> curves = new List<Curve> { curve };

            try
            {
                Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType, hookType, 
                    host, distributionDir, curves, hookOrientStart, hookOrientEnd, true, true);

                if (rebar != null)
                {
                    rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(count, distributionLength, true, true, true);
                }
            }
            catch { }
        }
    }
}
