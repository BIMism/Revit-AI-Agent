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

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder logic
            TaskDialog td = new TaskDialog("Foundation Rebar");
            td.TitleAutoPrefix = false;
            td.Title = "BIMism AI Agent";
            td.MainInstruction = "Reinforcement Generation";
            td.MainContent = "Configuration saved! Actual geometry generation is the next step.";
            td.Show();
            
            this.Close();
        }
    }
}
