using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    [Transaction(TransactionMode.Manual)]
    public class CmdModelCheck : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Model Check", "Model Check Tool: Coming Soon!\n\nThis feature will verify your model against BIM standards.");
            return Result.Succeeded;
        }
    }
}
