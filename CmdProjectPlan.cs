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
            TaskDialog td = new TaskDialog("Project Plan");
            td.TitleAutoPrefix = false;
            td.Title = "BIMism AI Agent";
            td.MainInstruction = "Project Plan feature is coming soon!";
            td.Show();
            return Result.Succeeded;
        }
    }
}
