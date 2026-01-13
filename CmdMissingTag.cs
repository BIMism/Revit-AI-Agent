using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    [Transaction(TransactionMode.Manual)]
    public class CmdMissingTag : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Missing Tag", "Missing Tag Tool: Coming Soon!\n\nThis feature will help you identify untagged elements.");
            return Result.Succeeded;
        }
    }
}
