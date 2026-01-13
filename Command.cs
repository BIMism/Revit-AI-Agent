using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            
            try
            {
                // Retrieve the Dockable Pane we registered in App.cs
                DockablePane pane = uiapp.GetDockablePane(App.PaneId);
                
                // Check if pane exists and handle visibility
                if (pane != null)
                {
                    // If hidden, show it. If already shown, bring to front
                    if (!pane.IsShown())
                    {
                        pane.Show();
                    }
                    else
                    {
                        // Already visible, just bring to focus
                        pane.Show();
                    }
                }
                else
                {
                    TaskDialog.Show("Error", "AI Agent pane not found. Please restart Revit.");
                    return Result.Failed;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Failed to show AI Sidebar: " + ex.Message);
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
