using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public static class RevitAI
    {
        // 1. SELECTION
        public static void Select(Document doc, IEnumerable<Element> elements)
        {
            if (elements == null) return;
            UIDocument uidoc = new UIDocument(doc);
            uidoc.Selection.SetElementIds(elements.Select(e => e.Id).ToList());
        }

        public static List<Element> GetAll(Document doc, BuiltInCategory category)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(category)
                .ToList();
        }

        public static List<Wall> GetWalls(Document doc) => GetAll(doc, BuiltInCategory.OST_Walls).Cast<Wall>().ToList();
        public static List<FamilyInstance> GetDoors(Document doc) => GetAll(doc, BuiltInCategory.OST_Doors).Cast<FamilyInstance>().ToList();
        public static List<FamilyInstance> GetWindows(Document doc) => GetAll(doc, BuiltInCategory.OST_Windows).Cast<FamilyInstance>().ToList();
        public static List<Element> GetFoundations(Document doc) => GetAll(doc, BuiltInCategory.OST_StructuralFoundation);
        public static List<Element> GetColumns(Document doc) => GetAll(doc, BuiltInCategory.OST_StructuralColumns);
        public static List<Element> GetBeams(Document doc) => GetAll(doc, BuiltInCategory.OST_StructuralFraming);

        // 2. RETRIEVAL
        public static Level GetLevel(Document doc, string name = null)
        {
            var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>();
            if (string.IsNullOrEmpty(name)) return levels.First();
            return levels.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? levels.First();
        }

        public static List<Level> GetLevels(Document doc) => new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();

        public static WallType GetWallType(Document doc, string name = null)
        {
            var types = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>();
            if (string.IsNullOrEmpty(name)) return types.First();
            return types.FirstOrDefault(t => t.Name.Contains(name)) ?? types.First();
        }

        public static List<WallType> GetWallTypes(Document doc) => new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().ToList();

        // 3. CREATION
        public static Wall CreateWall(Document doc, XYZ p1, XYZ p2, Level level = null, WallType type = null, double height = 10.0)
        {
            if (level == null) level = GetLevel(doc);
            if (type == null) type = GetWallType(doc);
            
            // Fix: Flatten Z to ensure horizontal line (Revit 2025 requirement)
            XYZ start = new XYZ(p1.X, p1.Y, 0);
            XYZ end = new XYZ(p2.X, p2.Y, 0);
            
            Line line = Line.CreateBound(start, end);
            return Wall.Create(doc, line, type.Id, level.Id, height, 0, false, false);
        }

        public static FamilyInstance CreateWindow(Document doc, Wall host, FamilySymbol symbol, double distanceAlongWall, double offsetFromLevel)
        {
            if (symbol == null) return null;
            if (!symbol.IsActive) symbol.Activate();

            LocationCurve locCurve = host.Location as LocationCurve;
            Curve curve = locCurve.Curve;
            
            // Calculate point along wall
            XYZ point = curve.Evaluate(distanceAlongWall / curve.Length, true);
            
            // Add Z offset for sill height
            XYZ finalPoint = new XYZ(point.X, point.Y, offsetFromLevel);

            return doc.Create.NewFamilyInstance(finalPoint, symbol, host, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }

        public static void CreateBox(Document doc, double size = 10.0)
        {
            Level level = GetLevel(doc);
            WallType type = GetWallType(doc);
            XYZ p1 = new XYZ(0, 0, 0);
            XYZ p2 = new XYZ(size, 0, 0);
            XYZ p3 = new XYZ(size, size, 0);
            XYZ p4 = new XYZ(0, size, 0);

            CreateWall(doc, p1, p2, level, type);
            CreateWall(doc, p2, p3, level, type);
            CreateWall(doc, p3, p4, level, type);
            CreateWall(doc, p4, p1, level, type);
        }

        public static List<Room> GetRooms(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Room))
                .Cast<Room>()
                .ToList();
        }

        public static Room CreateRoom(Document doc, XYZ point)
        {
            ViewPlan view = doc.ActiveView as ViewPlan ?? new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
            if (view == null || view.GenLevel == null) throw new Exception("No valid Floor Plan with Level found.");
            return doc.Create.NewRoom(view.GenLevel, new UV(point.X, point.Y));
        }

        // 5. DELETION
        public static void Delete(Document doc, IEnumerable<Element> elements)
        {
            if (elements == null) return;
            foreach (var e in elements)
            {
                try { doc.Delete(e.Id); } catch { }
            }
        }

        public static void DeleteAll(Document doc, BuiltInCategory category)
        {
            var ids = new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType().ToElementIds();
            if (ids.Any()) doc.Delete(ids);
        }

        public static void DeleteAll(Document doc, Type type)
        {
             var ids = new FilteredElementCollector(doc).OfClass(type).WhereElementIsNotElementType().ToElementIds();
             if (ids.Any()) doc.Delete(ids);
        }

        public static List<Element> GetSelection(Document doc)
        {
            UIDocument uidoc = new UIDocument(doc);
            return uidoc.Selection.GetElementIds().Select(id => doc.GetElement(id)).ToList();
        }
        // 6. FAMILIES
        public static FamilySymbol GetFamilySymbol(Document doc, string familyName, string typeName = null)
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));
            if (!string.IsNullOrEmpty(typeName))
                return collector.Cast<FamilySymbol>().FirstOrDefault(x => x.FamilyName.Contains(familyName) && x.Name.Contains(typeName));
            return collector.Cast<FamilySymbol>().FirstOrDefault(x => x.FamilyName.Contains(familyName));
        }

        public static void LoadFamily(Document doc, string path)
        {
            doc.LoadFamily(path);
        }

        public static void PlaceInstance(Document doc, string familyName, XYZ point)
        {
            FamilySymbol symbol = GetFamilySymbol(doc, familyName);
            if (symbol == null) return;
            if (!symbol.IsActive) symbol.Activate();
            doc.Create.NewFamilyInstance(point, symbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }

        // 7. PARAMETERS & DATA
        public static void SetParameter(Document doc, Element e, string paramName, string value)
        {
            Parameter p = e.LookupParameter(paramName);
            if (p != null && !p.IsReadOnly) p.Set(value);
        }

        public static void SetParameter(Document doc, Element e, string paramName, double value)
        {
            Parameter p = e.LookupParameter(paramName);
            if (p != null && !p.IsReadOnly) p.Set(value);
        }

        // 8. CLASH DETECTION (Simple)
        public static List<Element> GetClashes(Document doc, BuiltInCategory cat1, BuiltInCategory cat2)
        {
            var set1 = new FilteredElementCollector(doc).OfCategory(cat1).WhereElementIsNotElementType().ToElements();
            var set2Id = new FilteredElementCollector(doc).OfCategory(cat2).WhereElementIsNotElementType().ToElementIds().ToList();
            
            List<Element> clashes = new List<Element>();
            foreach (var e1 in set1)
            {
                var filter = new ElementIntersectsElementFilter(e1);
                var collisions = new FilteredElementCollector(doc, set2Id).WherePasses(filter).ToElements();
                if (collisions.Count > 0) clashes.Add(e1);
            }
            return clashes;
        }

        // 10. UNITS
        public static double MetersToFeet(double meters) => meters * 3.28084;
        public static double MMToFeet(double mm) => mm / 304.8;

        public static void CreateBox(Document doc, XYZ center, double width, double depth, double height)
        {
            Level level = GetLevel(doc);
            WallType type = GetWallType(doc);
            
            double w2 = width / 2;
            double d2 = depth / 2;
            
            XYZ p1 = new XYZ(center.X - w2, center.Y - d2, 0);
            XYZ p2 = new XYZ(center.X + w2, center.Y - d2, 0);
            XYZ p3 = new XYZ(center.X + w2, center.Y + d2, 0);
            XYZ p4 = new XYZ(center.X - w2, center.Y + d2, 0);

            CreateWall(doc, p1, p2, level, type, height);
            CreateWall(doc, p2, p3, level, type, height);
            CreateWall(doc, p3, p4, level, type, height);
            CreateWall(doc, p4, p1, level, type, height);
        }
    }

    public static class RevitExtensions
    {
        public static CurveArray ToCurveArray(this IEnumerable<Element> elements)
        {
            CurveArray array = new CurveArray();
            foreach (var e in elements)
            {
                if (e is Wall w && w.Location is LocationCurve lc)
                    array.Append(lc.Curve);
            }
            return array;
        }

        public static List<XYZ> GetPoints(this Element e)
        {
            var pts = new List<XYZ>();
            if (e is Wall w) {
                if (w.Location is LocationCurve lc) {
                    pts.Add(lc.Curve.GetEndPoint(0));
                    pts.Add(lc.Curve.GetEndPoint(1));
                }
            } else if (e.Location is LocationPoint lp) {
                pts.Add(lp.Point);
            }
            return pts;
        }
    }
}
