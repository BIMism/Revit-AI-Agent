using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public partial class FoundationRebarWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;

        public FoundationRebarWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            LoadRevitData();
            LoadIcons();
        }

        private void LoadIcons()
        {
            try
            {
                // Explicitly load images using Pack URI in code-behind
                ImgIsolated.Source = GetImage("footing.png");
                ImgCombined.Source = GetImage("combined.png");
                ImgStrap.Source = GetImage("strap.png");
                ImgRaft.Source = GetImage("raft.png");
                ImgPile.Source = GetImage("pile.png");
                ImgPileCap.Source = GetImage("pile_cap.png");
                ImgStrip.Source = GetImage("strip.png");
            }
            catch (Exception ex)
            {
                // Fail silently or log
            }
        }

        private System.Windows.Media.ImageSource GetImage(string name)
        {
            try
            {
                return new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/RevitAIAgent;component/Assets/{name}"));
            }
            catch
            {
                return null;
            }
        }

        private void LoadRevitData()
        {
            // Load Rebar Bar Types
            var barTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .OrderBy(x => x.Name)
                .ToList();

            ComboBarTypeX.ItemsSource = barTypes;
            ComboBarTypeX.DisplayMemberPath = "Name";
            
            ComboBarTypeY.ItemsSource = barTypes;
            ComboBarTypeY.DisplayMemberPath = "Name";

            // Bind Top Bars
            ComboTopBarTypeX.ItemsSource = barTypes;
            ComboTopBarTypeX.DisplayMemberPath = "Name";
            ComboTopBarTypeY.ItemsSource = barTypes;
            ComboTopBarTypeY.DisplayMemberPath = "Name";

            // Bind Dowels and Stirrups
            ComboDowelBarType.ItemsSource = barTypes;
            ComboDowelBarType.DisplayMemberPath = "Name";
            ComboStirrupBarType.ItemsSource = barTypes;
            ComboStirrupBarType.DisplayMemberPath = "Name";

            if (barTypes.Count > 0)
            {
                ComboBarTypeX.SelectedIndex = 0;
                ComboBarTypeY.SelectedIndex = 0;
                ComboTopBarTypeX.SelectedIndex = 0;
                ComboTopBarTypeY.SelectedIndex = 0;
                ComboDowelBarType.SelectedIndex = 0;
                ComboStirrupBarType.SelectedIndex = 0;
            }

            // Load Hook Types
            var hookTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarHookType))
                .Cast<RebarHookType>()
                .OrderBy(x => x.Name)
                .ToList();

            // Add "None" option if possible or handle null
            ComboHookX.ItemsSource = hookTypes;
            ComboHookX.DisplayMemberPath = "Name";
            
            ComboHookY.ItemsSource = hookTypes;
            ComboHookY.DisplayMemberPath = "Name";

            // Bind Top Hooks
            ComboTopHookX.ItemsSource = hookTypes;
            ComboTopHookX.DisplayMemberPath = "Name";
            ComboTopHookY.ItemsSource = hookTypes;
            ComboTopHookY.DisplayMemberPath = "Name";

            // Bind Dowel Hooks
            ComboDowelHookBase.ItemsSource = hookTypes;
            ComboDowelHookBase.DisplayMemberPath = "Name";
            
             if (hookTypes.Count > 0)
            {
                ComboHookX.SelectedIndex = 0;
                ComboHookY.SelectedIndex = 0;
                ComboTopHookX.SelectedIndex = 0;
                ComboTopHookY.SelectedIndex = 0;
                ComboDowelHookBase.SelectedIndex = 0;
            }

            // Load Covers (RebarCoverType)
             var coverTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarCoverType))
                .Cast<RebarCoverType>()
                .OrderBy(x => x.Name)
                .ToList();

            ComboCoverTop.ItemsSource = coverTypes;
            ComboCoverTop.DisplayMemberPath = "Name";
            ComboCoverBottom.ItemsSource = coverTypes;
            ComboCoverBottom.DisplayMemberPath = "Name";
            ComboCoverSide.ItemsSource = coverTypes;
            ComboCoverSide.DisplayMemberPath = "Name";

             if (coverTypes.Count > 0)
            {
                ComboCoverTop.SelectedIndex = 0;
                ComboCoverBottom.SelectedIndex = 0;
                ComboCoverSide.SelectedIndex = 0;
            }
        }

        private void BtnIsolated_Click(object sender, RoutedEventArgs e)
        {
            TypeSelectionGrid.Visibility = System.Windows.Visibility.Collapsed;
            DetailGrid.Visibility = System.Windows.Visibility.Visible;
            TitleText.Text = "Isolated Footing Reinforcement";
        }

        private void BtnComingSoon_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This foundation type is coming soon!", "BIMism AI Agent", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DetailGrid.Visibility = System.Windows.Visibility.Collapsed;
            TypeSelectionGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void SidebarList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelGeometry == null) return; // Prevent init crash

            // Hide all
            PanelGeometry.Visibility = System.Windows.Visibility.Collapsed;
            PanelBottomBars.Visibility = System.Windows.Visibility.Collapsed;
            PanelTopBars.Visibility = System.Windows.Visibility.Collapsed;
            PanelDowels.Visibility = System.Windows.Visibility.Collapsed;
            PanelStirrups.Visibility = System.Windows.Visibility.Collapsed;

            // Show selected
            ListBoxItem selectedItem = SidebarList.SelectedItem as ListBoxItem;
            if (selectedItem == null) return;

            string content = selectedItem.Content.ToString();
            switch (content)
            {
                case "Geometry": PanelGeometry.Visibility = System.Windows.Visibility.Visible; break;
                case "Bottom bars": PanelBottomBars.Visibility = System.Windows.Visibility.Visible; break;
                case "Top bars": PanelTopBars.Visibility = System.Windows.Visibility.Visible; break;
                case "Dowels": PanelDowels.Visibility = System.Windows.Visibility.Visible; break;
                case "Stirrups": PanelStirrups.Visibility = System.Windows.Visibility.Visible; break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Collect Configuration
                IsolatedRebarConfig config = new IsolatedRebarConfig();

                // Bottom Bars
                config.BottomBarX = ComboBarTypeX.SelectedItem as RebarBarType;
                config.BottomBarY = ComboBarTypeY.SelectedItem as RebarBarType;
                
                if (double.TryParse(InputSpacingX.Text, out double sx)) config.SpacingBottomX = sx;
                if (double.TryParse(InputSpacingY.Text, out double sy)) config.SpacingBottomY = sy;

                config.HookBottomX = ComboHookX.SelectedItem as RebarHookType;
                config.HookBottomY = ComboHookY.SelectedItem as RebarHookType;

                if (CheckOverrideHookX.IsChecked == true && double.TryParse(InputHookLenX.Text, out double hx)) config.OverrideHookLenBottomX = hx;
                if (CheckOverrideHookY.IsChecked == true && double.TryParse(InputHookLenY.Text, out double hy)) config.OverrideHookLenBottomY = hy;

                // Top Bars
                config.TopBarsEnabled = CheckAddTopBars.IsChecked == true;
               
                // For now, check if inputs valid
                config.TopBarX = ComboTopBarTypeX.SelectedItem as RebarBarType;
                config.TopBarY = ComboTopBarTypeY.SelectedItem as RebarBarType;
                if (double.TryParse(InputTopSpacingX.Text, out double tsx)) config.SpacingTopX = tsx;
                if (double.TryParse(InputTopSpacingY.Text, out double tsy)) config.SpacingTopY = tsy;
                config.HookTopX = ComboTopHookX.SelectedItem as RebarHookType;
                config.HookTopY = ComboTopHookY.SelectedItem as RebarHookType;

                if (CheckOverrideTopHookX.IsChecked == true && double.TryParse(InputTopHookLenX.Text, out double thx)) config.OverrideHookLenTopX = thx;
                if (CheckOverrideTopHookY.IsChecked == true && double.TryParse(InputTopHookLenY.Text, out double thy)) config.OverrideHookLenTopY = thy;

                // Dowels
                config.DowelsEnabled = CheckAddDowels.IsChecked == true;
                config.DowelBarType = ComboDowelBarType.SelectedItem as RebarBarType;
                config.DowelHookBase = ComboDowelHookBase.SelectedItem as RebarHookType;
                if (int.TryParse(InputDowelCount.Text, out int dc)) config.DowelCount = dc;
                if (double.TryParse(InputDowelLength.Text, out double dl)) config.DowelLength = dl;

                // Stirrups
                config.StirrupsEnabled = CheckAddStirrups.IsChecked == true;
                config.StirrupBarType = ComboStirrupBarType.SelectedItem as RebarBarType;
                if (double.TryParse(InputStirrupSpacing.Text, out double ss)) config.StirrupSpacing = ss;

                // 2. Access Selection
                List<Element> foundations = new List<Element>();
                var selectedIds = _uiDoc.Selection.GetElementIds();
                foreach (var id in selectedIds)
                {
                    Element elem = _doc.GetElement(id);
                    if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation)
                    {
                        foundations.Add(elem);
                    }
                }

                // If no pre-selection, prompt user to pick
                if (foundations.Count == 0)
                {
                    TaskDialog.Show("Revit AI Agent", "Please select at least one Isolated Footing.");
                    return;
                }

                // 3. Generate
                IsolatedRebarGenerator generator = new IsolatedRebarGenerator();
                generator.Generate(_doc, foundations, config);

                TaskDialog.Show("Revit AI Agent", $"Generated rebar for {foundations.Count} footing(s)!");
                this.Close();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Failed to generate rebar: " + ex.Message);
            }
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
