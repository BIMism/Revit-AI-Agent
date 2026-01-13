using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    [Transaction(TransactionMode.Manual)]
    public class CmdProjectPlan : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Project Plan", "Project Plan feature is coming soon!");
            return Result.Succeeded;
        }
    }
}
