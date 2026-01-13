using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    [Transaction(TransactionMode.Manual)]
    public class CmdClashCheck : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog td = new TaskDialog("Clash Check");
            td.TitleAutoPrefix = false;
            td.Title = "BIMism AI Agent";
            td.MainInstruction = "Clash Check feature is coming soon!";
            td.Show();
            return Result.Succeeded;
        }
    }
}
