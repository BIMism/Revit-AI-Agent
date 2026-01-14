using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public class App : IExternalApplication
    {
        public static DockablePaneId PaneId = new DockablePaneId(new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
        public static ChatView ChatPageInstance { get; set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Assembly resolution for dependencies
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    string assemblyName = new AssemblyName(args.Name).Name;
                    string assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyName + ".dll");
                    return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
                };

                // Register Dockable Pane
                try
                {
                    RevitRequestHandler handler = new RevitRequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    ChatView chatView = new ChatView(exEvent, handler);
                    ChatPageInstance = chatView;
                    application.RegisterDockablePane(PaneId, "BIM'ism Copilot", chatView);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to register dockable pane: " + ex.Message);
                }

                // Create Ribbon Tab
                string tabName = "BIMism";
                try { application.CreateRibbonTab(tabName); } catch { }

                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // UI Panels Creation
                try
                {
                    // PANEL 1: AI AGENT
                    RibbonPanel panelAI = application.CreateRibbonPanel(tabName, "AI Agent");
                    
                    PushButtonData btnDataAI = new PushButtonData("AI_Agent_Button", "BIM'ism AI", assemblyPath, "RevitAIAgent.Command");
                    PushButton btnAI = panelAI.AddItem(btnDataAI) as PushButton;
                    btnAI.ToolTip = "Launch the BIM'ism AI Agent";
                    SetButtonIcon(btnAI, "ai_agent.png");

                    // PANEL 4: BIMism (New Panel)
                    RibbonPanel panelBIMism = application.CreateRibbonPanel(tabName, "BIMism");

                    PushButtonData btnAboutData = new PushButtonData("About_Button", "About", assemblyPath, "RevitAIAgent.CmdAbout");
                    PushButton btnAbout = panelBIMism.AddItem(btnAboutData) as PushButton;
                    btnAbout.ToolTip = "Check version and updates";
                    SetButtonIcon(btnAbout, "about_v4.png");

                    // PANEL 5: Check (New Panel)
                    RibbonPanel panelCheck = application.CreateRibbonPanel(tabName, "Check");

                    PushButtonData btnModelCheckData = new PushButtonData("ModelCheck_Button", "Model\nCheck", assemblyPath, "RevitAIAgent.CmdModelCheck");
                    PushButton btnModelCheck = panelCheck.AddItem(btnModelCheckData) as PushButton;
                    btnModelCheck.ToolTip = "Verify model against standards";
                    SetButtonIcon(btnModelCheck, "model_check_v4.png");

                    PushButtonData btnDrawingCheckData = new PushButtonData("DrawingCheck_Button", "Drawing\nCheck", assemblyPath, "RevitAIAgent.CmdDrawingCheck");
                    PushButton btnDrawingCheck = panelCheck.AddItem(btnDrawingCheckData) as PushButton;
                    btnDrawingCheck.ToolTip = "Verify drawings and annotations";
                    SetButtonIcon(btnDrawingCheck, "drawing_check.png");

                    PushButtonData btnClashCheckData = new PushButtonData("ClashCheck_Button", "Clash\nCheck", assemblyPath, "RevitAIAgent.CmdClashCheck");
                    PushButton btnClashCheck = panelCheck.AddItem(btnClashCheckData) as PushButton;
                    btnClashCheck.ToolTip = "Detect clashes between elements";
                    SetButtonIcon(btnClashCheck, "clash_check.png");

                    // PANEL 2: STRUCTURAL (REBAR)
                    RibbonPanel panelStruct = application.CreateRibbonPanel(tabName, "Structural");
                    CreateButton(panelStruct, "BtnFoundationRebar", "Foundation\nRebar", assemblyPath, "RevitAIAgent.CmdFoundationRebar", "footing.png");
                    CreateButton(panelStruct, "BtnColumn", "Column\nRebar", assemblyPath, "RevitAIAgent.CmdColumn", "column.png");
                    CreateButton(panelStruct, "BtnBeam", "Beam\nRebar", assemblyPath, "RevitAIAgent.CmdBeam", "beam.png");
                    CreateButton(panelStruct, "BtnSlab", "Slab\nRebar", assemblyPath, "RevitAIAgent.CmdSlab", "slab.png");
                    CreateButton(panelStruct, "BtnWall", "Wall\nRebar", assemblyPath, "RevitAIAgent.CmdWall", "wall.png");
                    panelStruct.AddSeparator();
                    CreateButton(panelStruct, "BtnVis3D", "Show\n3D Solid", assemblyPath, "RevitAIAgent.CmdVisibility3D", "visibility.png");
                    CreateButton(panelStruct, "BtnWeight", "Rebar\nWeight", assemblyPath, "RevitAIAgent.CmdRebarWeight", "weight.png");
                    CreateButton(panelStruct, "BtnCoordinate", "Coordinate", assemblyPath, "RevitAIAgent.CmdCoordinate", "coordinate.png");

                    // PANEL 3: ANNOTATION
                    RibbonPanel panelAnnotation = application.CreateRibbonPanel(tabName, "Annotation");
                    CreateButton(panelAnnotation, "BtnElemDim", "Element\nDimension", assemblyPath, "RevitAIAgent.CmdElementDimension", "dimension.png");
                    CreateButton(panelAnnotation, "BtnMissingTag", "Missing\nTag", assemblyPath, "RevitAIAgent.CmdMissingTag", "missing_tag_v4.png"); 

                    // PANEL 6: PM (Project Management)
                    RibbonPanel panelPM = application.CreateRibbonPanel(tabName, "PM");

                    PushButtonData btnProjectPlanData = new PushButtonData("ProjectPlan_Button", "Project\nPlan", assemblyPath, "RevitAIAgent.CmdProjectPlan");
                    PushButton btnProjectPlan = panelPM.AddItem(btnProjectPlanData) as PushButton;
                    btnProjectPlan.ToolTip = "View and manage project plan";
                    SetButtonIcon(btnProjectPlan, "project_plan.png");

                    PushButtonData btnCostingData = new PushButtonData("Costing_Button", "Costing", assemblyPath, "RevitAIAgent.CmdCosting");
                    PushButton btnCosting = panelPM.AddItem(btnCostingData) as PushButton;
                    btnCosting.ToolTip = "Estimate project costs";
                    SetButtonIcon(btnCosting, "costing.png");

                    PushButtonData btnResourcesData = new PushButtonData("Resources_Button", "Resources", assemblyPath, "RevitAIAgent.CmdResources");
                    PushButton btnResources = panelPM.AddItem(btnResourcesData) as PushButton;
                    btnResources.ToolTip = "Manage project resources";
                    SetButtonIcon(btnResources, "resources.png");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("UI Error", "Failed to create one or more ribbon panels: " + ex.Message);
                }

                // Startup update check removed per user request (v2.0.6)
                // CheckForUpdatesAsync();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fatal Startup Error", "Add-In failed to load: " + ex.Message + "\n" + ex.StackTrace);
                return Result.Failed;
            }
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();
                if (updateInfo != null)
                {
                    // Show update notification
                    TaskDialog td = new TaskDialog("BIMism AI Agent");
                    td.TitleAutoPrefix = false;
                    td.Title = "BIMism AI Agent - Update Available";
                    td.MainInstruction = $"New version {updateInfo.Version} is available!";
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    td.MainContent = $"Current version: {currentVersion}\n\n{updateInfo.ReleaseNotes}\n\nWould you like to update now?";
                    td.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                    td.DefaultButton = TaskDialogResult.Yes;

                    if (td.Show() == TaskDialogResult.Yes)
                    {
                        // Show downloading message
                        TaskDialog downloading = new TaskDialog("BIMism AI Agent");
                        downloading.Title = "Downloading Update";
                        downloading.MainInstruction = "Downloading update...";
                        downloading.MainContent = "Please wait while the update is downloaded and installed.";
                        
                        bool success = await AutoUpdater.DownloadAndInstallAsync(updateInfo.DownloadUrl);
                        
                        if (success)
                        {
                            TaskDialog successTd = new TaskDialog("Success");
                            successTd.TitleAutoPrefix = false;
                            successTd.Title = "BIMism AI Agent";
                            successTd.MainInstruction = "Update installed successfully!";
                            successTd.MainContent = "Please restart Revit to use the new version.";
                            successTd.CommonButtons = TaskDialogCommonButtons.Ok;
                            successTd.Show();
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore update check failures
            }
        }

        private void CreateButton(RibbonPanel panel, string name, string text, string assemblyPath, string className, string iconFileName)
        {
            PushButtonData data = new PushButtonData(name, text, assemblyPath, className);
            PushButton btn = panel.AddItem(data) as PushButton;
            SetButtonIcon(btn, iconFileName);
        }

        private void SetButtonIcon(PushButton button, string iconFileName)
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                // When deployed in BIMism subfolder, Assets are parallel to valid dll
                string iconPath = Path.Combine(assemblyDir, "Assets", iconFileName);
                
                if (File.Exists(iconPath))
                {
                    BitmapImage bmp = new BitmapImage(new Uri(iconPath));
                    button.LargeImage = bmp;
                    button.Image = bmp; // Also set small icon for compatibility
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load icon {iconFileName}: {ex.Message}");
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
