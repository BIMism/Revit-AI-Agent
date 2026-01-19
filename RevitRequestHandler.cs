using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    // A simple enum to define what we want Revit to do
    public enum RequestId
    {
        None = 0,
        TagAllViewSpecific = 1,
        SelectAllInView = 2,
        AutoDimensionGrids = 3,
        CreateWalls = 4,
        ExecuteScript = 5,
        GetContext = 6
    }

    public class RevitRequestHandler : IExternalEventHandler
    {
        public RequestId Request { get; set; } = RequestId.None;
        
        // Static store for context (UI reads this)
        public static string LastContext { get; set; } = "";

        // Parameters for requests
        public int IntParam { get; set; }
        public double DoubleParam1 { get; set; }
        public double DoubleParam2 { get; set; }
        public string StringParam { get; set; }
        public string CurrentCommandId { get; set; } // Added for error reporting

        public void Execute(UIApplication uiapp)
        {
            GetProjectContext(uiapp); // Update context dump

            if (Request == RequestId.None) return;
            try
            {
                // SAFEGUARD: If no document is open, we can't do anything
                if (uiapp.ActiveUIDocument == null)
                {
                    // Just return, or maybe log. 
                    // For GetContext, we can set a default.
                    if (Request == RequestId.GetContext) LastContext = "No Active Document";
                    return;
                }

                switch (Request)
                {
                    case RequestId.GetContext:
                        GetProjectContext(uiapp);
                        break;
                    case RequestId.TagAllViewSpecific:
                        TagAllElementsAndRooms(uiapp);
                        break;
                    case RequestId.SelectAllInView:
                        SelectAllElements(uiapp);
                        break;
                    case RequestId.AutoDimensionGrids:
                        CreateGridDimensions(uiapp);
                        break;
                    case RequestId.CreateWalls:
                        CreateWalls(uiapp, IntParam, DoubleParam1, DoubleParam2);
                        break;
                    case RequestId.ExecuteScript:
                        bool success = RunScriptInTransaction(uiapp, StringParam);
                        UpdateBridgeStatus(success, success ? "Execution Complete" : "Execution Failed");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                UpdateBridgeStatus(false, ex.Message);
            }
            finally
            {
                Request = RequestId.None;
            }
        }

        private void UpdateBridgeStatus(bool success, string message)
        {
            try
            {
                string bridgeFile = @"C:\Temp\BIMism_Bridge.json";
                if (System.IO.File.Exists(bridgeFile))
                {
                    string json = System.IO.File.ReadAllText(bridgeFile);
                    var cmd = Newtonsoft.Json.JsonConvert.DeserializeObject<BridgeListener.BridgeCommand>(json);
                    if (cmd != null)
                    {
                        cmd.Status = success ? "SUCCESS" : "ERROR";
                        cmd.Result = message;
                        System.IO.File.WriteAllText(bridgeFile, Newtonsoft.Json.JsonConvert.SerializeObject(cmd));
                    }
                }
            }
            catch { /* Best effort */ }
        }

        public static void GetProjectContext(UIApplication uiapp)
        {
            if (uiapp.ActiveUIDocument == null) return;
            Document doc = uiapp.ActiveUIDocument.Document;

            try
            {
                var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Select(l => l.Name).ToList();
                var wallTypes = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Select(t => t.Name).Take(20).ToList();
                var floorTypes = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().Select(t => t.Name).Take(20).ToList();
                var views = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v => !v.IsTemplate).Select(v => v.Name).ToList();

                var contextData = new
                {
                    ActiveView = doc.ActiveView.Name,
                    Levels = levels,
                    WallTypes = wallTypes,
                    FloorTypes = floorTypes,
                    Views = views,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonConvert.SerializeObject(contextData, Formatting.Indented);
                string path = @"C:\Temp\BIMism_Context.json";
                if (!Directory.Exists(@"C:\Temp")) Directory.CreateDirectory(@"C:\Temp");
                File.WriteAllText(path, json);
            }
            catch { /* Ignore context errors */ }
        }

        public string GetName()
        {
            return "BIMism AI Request Handler";
        }

        // --- Implementation of Features ---

        private void SelectAllElements(UIApplication uiapp)
        {
            if (uiapp.ActiveUIDocument == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            // Select all model elements in current view
            FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id);
            ICollection<ElementId> ids = collector.WhereElementIsNotElementType().ToElementIds();

            uidoc.Selection.SetElementIds(ids);
            TaskDialog.Show("AI Agent", $"Selected {ids.Count} elements.");
        }

        public void CreateWalls(UIApplication uiapp, int count, double length, double width)
        {
            if (uiapp.ActiveUIDocument == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            using (Transaction t = new Transaction(doc, "AI Create Walls"))
            {
                t.Start();
                
                // Get a Wall Type (First available)
                WallType wType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .FirstElement() as WallType;
                    
                // Get a Level (Active view level or first level)
                Level level = activeView.GenLevel;
                if (level == null)
                {
                     level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .FirstElement() as Level;
                }

                if (wType != null && level != null)
                {
                    // Create a box of walls or just 'count' walls
                    // Let's create a simple rectangular room for now based on requests like "4 walls"
                    
                    // Simple logic: Draw a box centered at 0,0
                    double halfL = length / 2.0;
                    double halfW = width / 2.0;
                    
                    XYZ p1 = new XYZ(-halfL, -halfW, 0);
                    XYZ p2 = new XYZ(halfL, -halfW, 0);
                    XYZ p3 = new XYZ(halfL, halfW, 0);
                    XYZ p4 = new XYZ(-halfL, halfW, 0);
                    
                    List<Curve> profile = new List<Curve>
                    {
                        Line.CreateBound(p1, p2),
                        Line.CreateBound(p2, p3),
                        Line.CreateBound(p3, p4),
                        Line.CreateBound(p4, p1)
                    };
                    
                    foreach (Curve c in profile)
                    {
                         Wall.Create(doc, c, wType.Id, level.Id, 10.0, 0.0, false, false);
                    }
                    
                    TaskDialog.Show("Success", "Created user requests walls.");
                }

                t.Commit();
            }
        }

        private bool RunScriptInTransaction(UIApplication uiapp, string code)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            bool success = true;

            using (Transaction t = new Transaction(doc, "AI Dynamic Actions"))
            {
                t.Start();
                try
                {
                    string error = ScriptRunner.RunScript(uiapp, code);
                    if (error != null)
                    {
                         // Rollback if script failed
                         t.RollBack();
                         UpdateBridgeStatus(false, error);
                         return false;
                    }
                }
                catch (Exception ex)
                {
                    // UpdateBridgeStatus handles the logging/reporting via the return value
                    success = false;
                }
                t.Commit();
            }
            return success;
        }

        private void CreateGridDimensions(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            using (Transaction t = new Transaction(doc, "AI Auto-Dimension"))
            {
                t.Start();

                // Get all Grids in view
                FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id);
                IList<Element> grids = collector.OfClass(typeof(Grid)).ToElements();

                if (grids.Count < 2)
                {
                    TaskDialog.Show("Info", "Need at least 2 grids to dimension.");
                    return;
                }

                // Simple logic: Create a dimension line across the grids
                // We need a line to draw the dimension. Let's find a line perpendicular to grids.
                // Assuming vertical/horizontal grids for simplicity.
                
                try 
                {
                    ReferenceArray refArray = new ReferenceArray();
                    foreach (Grid g in grids)
                    {
                        refArray.Append(new Reference(g));
                    }

                    // Create a dummy line for dimension placement
                    // (In a real app, we'd calculate bounding boxes)
                    XYZ p1 = new XYZ(0, 0, 0);
                    XYZ p2 = new XYZ(100, 0, 0);
                    Line line = Line.CreateBound(p1, p2);

                    doc.Create.NewDimension(activeView, line, refArray);
                    TaskDialog.Show("Success", "Created Grid Dimensions.");
                }
                catch
                {
                     TaskDialog.Show("Info", "Could not automatically place dimensions. (Complex grid arrangement?)");
                }

                t.Commit();
            }
        }

        private void TagAllElementsAndRooms(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            // We can't tag in 3D views usually
            if (activeView.ViewType == ViewType.ThreeD)
            {
                TaskDialog.Show("Info", "Cannot tag in 3D view.");
                return;
            }

            using (Transaction t = new Transaction(doc, "AI Auto-Tag"))
            {
                t.Start();
                
                // 1. Tag Rooms (if any)
                // In Revit 2025, CreateTag might differ slightly, but usually it's independent tags
                // For simplicity, let's use the standard "Tag All Not Tagged" equivalent approach manually 
                // or just tag loop.
                
                // Simple approach: Tag all Doors and Windows in the view
                List<BuiltInCategory> cats = new List<BuiltInCategory> 
                { 
                    BuiltInCategory.OST_Doors, 
                    BuiltInCategory.OST_Windows,
                    BuiltInCategory.OST_RoomTags // Room Tags are different, handled separately usually
                };

                int tagCount = 0;

                foreach (var cat in cats)
                {
                    // Find elements in view
                    FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id);
                    collector.OfCategory(cat).WhereElementIsNotElementType();

                    foreach (Element e in collector)
                    {
                        // Check if already tagged? (Simple version: just try to create a tag)
                        // Note: To be robust, we should check existing tags.
                        
                        try
                        {
                            // Create Independent Tag
                            // 2018+ syntax. 
                            BoundingBoxXYZ bbox = e.get_BoundingBox(activeView);
                            if (bbox != null)
                            {
                                XYZ midpoint = (bbox.Min + bbox.Max) / 2.0;
                                
                                // TagHeadPosition and TagMode are arguments
                                // For Revit 2025, IndependentTag.Create arguments: (Document, ElementId, Reference, TagMode, TagOrientation, XYZ)
                                // We need to check if the category has a loaded tag family first, otherwise this throws.
                                
                                Reference refElem = new Reference(e);
                                IndependentTag newTag = IndependentTag.Create(
                                    doc, 
                                    activeView.Id, 
                                    refElem, 
                                    false, // Leader
                                    TagMode.TM_ADDBY_CATEGORY, 
                                    TagOrientation.Horizontal, 
                                    midpoint);
                                
                                tagCount++;
                            }
                        }
                        catch 
                        {
                            // Ignore failures for now (e.g. no tag loaded)
                        }
                    }
                }

                t.Commit();
                
                if (tagCount > 0)
                {
                    TaskDialog.Show("Success", $"AI Agent tagged {tagCount} elements.");
                }
                else
                {
                    TaskDialog.Show("Info", "No elements tagged. Ensure Tags are loaded for Doors/Windows.");
                }
            }
        }
    }
}
