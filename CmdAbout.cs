using System;
using System.Diagnostics;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    [Transaction(TransactionMode.Manual)]
    public class CmdAbout : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Show About dialog
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
            
            return Result.Succeeded;
        }
    }
}
