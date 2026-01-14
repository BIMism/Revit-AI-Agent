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

            if (barTypes.Count > 0)
            {
                ComboBarTypeX.SelectedIndex = 0;
                ComboBarTypeY.SelectedIndex = 0;
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
            
             if (hookTypes.Count > 0)
            {
                ComboHookX.SelectedIndex = 0;
                ComboHookY.SelectedIndex = 0;
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

        private void BtnPadFooting_Click(object sender, RoutedEventArgs e)
        {
            TypeSelectionGrid.Visibility = Visibility.Collapsed;
            DetailGrid.Visibility = Visibility.Visible;
            TitleText.Text = "Pad Footing Reinforcement";
        }

        private void BtnComingSoon_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This foundation type is coming soon!", "BIMism AI Agent", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DetailGrid.Visibility = Visibility.Collapsed;
            TypeSelectionGrid.Visibility = Visibility.Visible;
        }

        private void SidebarList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelGeometry == null) return; // Prevent init crash

            // Hide all
            PanelGeometry.Visibility = Visibility.Collapsed;
            PanelBottomBars.Visibility = Visibility.Collapsed;
            PanelTopBars.Visibility = Visibility.Collapsed;
            PanelDowels.Visibility = Visibility.Collapsed;
            PanelStirrups.Visibility = Visibility.Collapsed;

            // Show selected
            ListBoxItem selectedItem = SidebarList.SelectedItem as ListBoxItem;
            if (selectedItem == null) return;

            string content = selectedItem.Content.ToString();
            switch (content)
            {
                case "Geometry": PanelGeometry.Visibility = Visibility.Visible; break;
                case "Bottom bars": PanelBottomBars.Visibility = Visibility.Visible; break;
                case "Top bars": PanelTopBars.Visibility = Visibility.Visible; break;
                case "Dowels": PanelDowels.Visibility = Visibility.Visible; break;
                case "Stirrups": PanelStirrups.Visibility = Visibility.Visible; break;
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
