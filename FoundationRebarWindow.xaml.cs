using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public partial class FoundationRebarWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private List<RebarBarType> _barTypes;

        public FoundationRebarWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            LoadRevitData();
            LoadIcons();
            
            // Init Previews
            HookUpEvents();
            UpdatePreviews();
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
            catch { }
        }

        private ImageSource GetImage(string name)
        {
            try
            {
                return new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/RevitAIAgent;component/Assets/{name}"));
            }
            catch { return null; }
        }

        private void LoadRevitData()
        {
            // Load Rebar Bar Types
            _barTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .OrderBy(x => x.Name)
                .ToList();

            ComboBarTypeX.ItemsSource = _barTypes; ComboBarTypeX.DisplayMemberPath = "Name";
            ComboBarTypeY.ItemsSource = _barTypes; ComboBarTypeY.DisplayMemberPath = "Name";
            ComboTopBarTypeX.ItemsSource = _barTypes; ComboTopBarTypeX.DisplayMemberPath = "Name";
            ComboTopBarTypeY.ItemsSource = _barTypes; ComboTopBarTypeY.DisplayMemberPath = "Name";
            ComboDowelBarType.ItemsSource = _barTypes; ComboDowelBarType.DisplayMemberPath = "Name";
            ComboStirrupBarType.ItemsSource = _barTypes; ComboStirrupBarType.DisplayMemberPath = "Name";
            
            // Additional Bars
            ComboAddB1Type.ItemsSource = _barTypes; ComboAddB1Type.DisplayMemberPath = "Name"; 
            ComboAddB2Type.ItemsSource = _barTypes; ComboAddB2Type.DisplayMemberPath = "Name"; 
            ComboAddT1Type.ItemsSource = _barTypes; ComboAddT1Type.DisplayMemberPath = "Name"; 
            ComboAddT2Type.ItemsSource = _barTypes; ComboAddT2Type.DisplayMemberPath = "Name"; 

            if (_barTypes.Count > 0)
            {
                ComboBarTypeX.SelectedIndex = 0; ComboBarTypeY.SelectedIndex = 0;
                ComboTopBarTypeX.SelectedIndex = 0; ComboTopBarTypeY.SelectedIndex = 0;
                ComboDowelBarType.SelectedIndex = 0; ComboStirrupBarType.SelectedIndex = 0;
                ComboAddB1Type.SelectedIndex = 0; ComboAddB2Type.SelectedIndex = 0;
                ComboAddT1Type.SelectedIndex = 0; ComboAddT2Type.SelectedIndex = 0;
            }

            // Load Hook Types
            var hookTypes = new FilteredElementCollector(_doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>().OrderBy(x => x.Name).ToList();

            ComboHookX.ItemsSource = hookTypes; ComboHookX.DisplayMemberPath = "Name";
            ComboHookY.ItemsSource = hookTypes; ComboHookY.DisplayMemberPath = "Name";
            ComboTopHookX.ItemsSource = hookTypes; ComboTopHookX.DisplayMemberPath = "Name";
            ComboTopHookY.ItemsSource = hookTypes; ComboTopHookY.DisplayMemberPath = "Name";
            ComboDowelHookBase.ItemsSource = hookTypes; ComboDowelHookBase.DisplayMemberPath = "Name";
            
             if (hookTypes.Count > 0)
            {
                ComboHookX.SelectedIndex = 0; ComboHookY.SelectedIndex = 0;
                ComboTopHookX.SelectedIndex = 0; ComboTopHookY.SelectedIndex = 0;
                ComboDowelHookBase.SelectedIndex = 0;
            }

            // Load Covers (RebarCoverType)
             var coverTypes = new FilteredElementCollector(_doc).OfClass(typeof(RebarCoverType)).Cast<RebarCoverType>().OrderBy(x => x.Name).ToList();

            ComboCoverTop.ItemsSource = coverTypes; ComboCoverTop.DisplayMemberPath = "Name";
            ComboCoverBottom.ItemsSource = coverTypes; ComboCoverBottom.DisplayMemberPath = "Name";
            ComboCoverSide.ItemsSource = coverTypes; ComboCoverSide.DisplayMemberPath = "Name";

             if (coverTypes.Count > 0)
            {
                ComboCoverTop.SelectedIndex = 0; ComboCoverBottom.SelectedIndex = 0; ComboCoverSide.SelectedIndex = 0;
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
            PanelB1.Visibility = System.Windows.Visibility.Collapsed;
            PanelB2.Visibility = System.Windows.Visibility.Collapsed;
            PanelT1.Visibility = System.Windows.Visibility.Collapsed;
            PanelT2.Visibility = System.Windows.Visibility.Collapsed;
            PanelDowels.Visibility = System.Windows.Visibility.Collapsed;
            PanelStirrups.Visibility = System.Windows.Visibility.Collapsed;

            // Show selected
            int index = SidebarList.SelectedIndex;
            switch (index)
            {
                case 0: PanelGeometry.Visibility = System.Windows.Visibility.Visible; break;
                case 1: PanelB1.Visibility = System.Windows.Visibility.Visible; break;
                case 2: PanelB2.Visibility = System.Windows.Visibility.Visible; break;
                case 3: PanelT1.Visibility = System.Windows.Visibility.Visible; break;
                case 4: PanelT2.Visibility = System.Windows.Visibility.Visible; break;
                case 5: PanelDowels.Visibility = System.Windows.Visibility.Visible; break;
                case 6: PanelStirrups.Visibility = System.Windows.Visibility.Visible; break;
            }
            
            // Trigger update
            UpdatePreviews();
        }
        
        private void UpdatePreviews()
        {
            if (CanvasSectionMajor == null || CanvasSectionMinor == null) return;
            if (CheckDynamicUpdate != null && CheckDynamicUpdate.IsChecked == false) return;

            DrawSection(CanvasSectionMajor, true);  // Major (B1, T1)
            DrawSection(CanvasSectionMinor, false); // Minor (B2, T2)
        }

        private void DrawSection(Canvas canvas, bool isMajor)
        {
            canvas.Children.Clear();
            double cw = canvas.Width, ch = canvas.Height;
            double fw = 200, fh = 70;
            double x0 = (cw - fw) / 2, y0 = (ch - fh) / 2 + 10;

            // Footing Rect
            System.Windows.Shapes.Rectangle footing = new System.Windows.Shapes.Rectangle
            {
                Width = fw, Height = fh, Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)), 
                Stroke = System.Windows.Media.Brushes.DimGray, StrokeThickness = 2
            };
            Canvas.SetLeft(footing, x0); Canvas.SetTop(footing, y0);
            canvas.Children.Add(footing);

            // Rebar Logic
            bool hasTop = CheckAddTopBars?.IsChecked == true;
            bool hasDowels = CheckAddDowels?.IsChecked == true;
            var redBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 20, 20));

            // Bottom Bars (Section Dots for cross, Line for main)
            double bz = y0 + fh - 10;
            for(int i=0; i<8; i++)
            {
                System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse { Width=5, Height=5, Fill=redBrush };
                Canvas.SetLeft(dot, x0 + 15 + i * (fw-30)/7 - 2.5);
                Canvas.SetTop(dot, bz - (isMajor ? 0 : 7) - 2.5);
                canvas.Children.Add(dot);
            }
            
            // Bottom Main Line (with Hooks)
            System.Windows.Shapes.Polyline bMain = new System.Windows.Shapes.Polyline
            {
                Stroke = redBrush, StrokeThickness = 2.5,
                Points = new PointCollection { 
                    new System.Windows.Point(x0+8, y0+fh-30), 
                    new System.Windows.Point(x0+8, y0+fh-8 - (isMajor ? 7 : 0)), 
                    new System.Windows.Point(x0+fw-8, y0+fh-8 - (isMajor ? 7 : 0)), 
                    new System.Windows.Point(x0+fw-8, y0+fh-30) 
                }
            };
            canvas.Children.Add(bMain);

            // Top Bars
            if (hasTop)
            {
                double tz = y0 + 10;
                for(int i=0; i<8; i++)
                {
                    System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse { Width=5, Height=5, Fill=redBrush };
                    Canvas.SetLeft(dot, x0 + 15 + i * (fw-30)/7 - 2.5);
                    Canvas.SetTop(dot, tz + (isMajor ? 0 : 7) - 2.5);
                    canvas.Children.Add(dot);
                }

                System.Windows.Shapes.Polyline tLine = new System.Windows.Shapes.Polyline
                {
                    Stroke = redBrush, StrokeThickness = 2.5,
                    Points = new PointCollection { 
                        new System.Windows.Point(x0+8, y0+30), 
                        new System.Windows.Point(x0+8, y0+8 + (isMajor ? 7 : 0)), 
                        new System.Windows.Point(x0+fw-8, y0+8 + (isMajor ? 7 : 0)), 
                        new System.Windows.Point(x0+fw-8, y0+30) 
                    }
                };
                canvas.Children.Add(tLine);
            }

            // Dowels
            if (hasDowels)
            {
                var greyBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
                for(int i=0; i<2; i++)
                {
                    double dx = x0 + 60 + i * (fw-120);
                    System.Windows.Shapes.Line dowel = new System.Windows.Shapes.Line
                    {
                        X1 = dx, Y1 = y0 - 30, X2 = dx, Y2 = y0 + fh - 15,
                        Stroke = greyBrush, StrokeThickness = 4
                    };
                    canvas.Children.Add(dowel);
                }
            }

            // Labels
            TextBlock label = new TextBlock { Text = isMajor ? "SECTION X-X" : "SECTION Y-Y", FontWeight=FontWeights.Bold, FontSize=10, Foreground=System.Windows.Media.Brushes.DimGray };
            Canvas.SetLeft(label, x0); Canvas.SetTop(label, y0 + fh + 5);
            canvas.Children.Add(label);
        }

        private void HookUpEvents()
        {
            CheckAddTopBars.Checked += (s, e) => UpdatePreviews();
            CheckAddTopBars.Unchecked += (s, e) => UpdatePreviews();
            CheckAddDowels.Checked += (s, e) => UpdatePreviews();
            CheckAddDowels.Unchecked += (s, e) => UpdatePreviews();
            
            CheckAdditionalB1.Checked += (s, e) => UpdatePreviews();
            CheckAdditionalB1.Unchecked += (s, e) => UpdatePreviews();
            CheckAdditionalB2.Checked += (s, e) => UpdatePreviews();
            CheckAdditionalB2.Unchecked += (s, e) => UpdatePreviews();
            CheckAdditionalT1.Checked += (s, e) => UpdatePreviews();
            CheckAdditionalT1.Unchecked += (s, e) => UpdatePreviews();
            CheckAdditionalT2.Checked += (s, e) => UpdatePreviews();
            CheckAdditionalT2.Unchecked += (s, e) => UpdatePreviews();

            InputAddB1Count.TextChanged += (s, e) => UpdatePreviews();
            InputAddB2Count.TextChanged += (s, e) => UpdatePreviews();
            InputDowelCount.TextChanged += (s, e) => UpdatePreviews();
            InputSpacingX.TextChanged += (s, e) => UpdatePreviews();
            InputSpacingY.TextChanged += (s, e) => UpdatePreviews();
            
            SidebarList.SelectionChanged += (s, e) => UpdatePreviews();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsolatedRebarConfig config = new IsolatedRebarConfig();

                // Bottom Bars (B1, B2)
                config.BottomBarX = ComboBarTypeX.SelectedItem as RebarBarType;
                config.BottomBarY = ComboBarTypeY.SelectedItem as RebarBarType;
                if (double.TryParse(InputSpacingX.Text, out double sx)) config.SpacingBottomX = sx;
                if (double.TryParse(InputSpacingY.Text, out double sy)) config.SpacingBottomY = sy;
                config.HookBottomX = ComboHookX.SelectedItem as RebarHookType;
                config.HookBottomY = ComboHookY.SelectedItem as RebarHookType;
                if (CheckOverrideHookX.IsChecked == true && double.TryParse(InputHookLenX.Text, out double hx)) config.OverrideHookLenBottomX = hx;
                if (CheckOverrideHookY.IsChecked == true && double.TryParse(InputHookLenY.Text, out double hy)) config.OverrideHookLenBottomY = hy;

                // Additional B1
                config.AddB1Enabled = CheckAdditionalB1.IsChecked == true;
                config.AddB1Type = ComboAddB1Type.SelectedItem as RebarBarType;
                if (int.TryParse(InputAddB1Count.Text, out int ac1)) config.AddB1Count = ac1;

                // Additional B2
                config.AddB2Enabled = CheckAdditionalB2.IsChecked == true;
                config.AddB2Type = ComboAddB2Type.SelectedItem as RebarBarType;
                if (int.TryParse(InputAddB2Count.Text, out int ac2)) config.AddB2Count = ac2;


                // Top Bars (T1, T2)
                config.TopBarsEnabled = CheckAddTopBars.IsChecked == true;
                config.TopBarX = ComboTopBarTypeX.SelectedItem as RebarBarType;
                config.TopBarY = ComboTopBarTypeY.SelectedItem as RebarBarType;
                if (double.TryParse(InputTopSpacingX.Text, out double tsx)) config.SpacingTopX = tsx;
                if (double.TryParse(InputTopSpacingY.Text, out double tsy)) config.SpacingTopY = tsy;
                config.HookTopX = ComboTopHookX.SelectedItem as RebarHookType;
                config.HookTopY = ComboTopHookY.SelectedItem as RebarHookType;
                if (CheckOverrideTopHookX.IsChecked == true && double.TryParse(InputTopHookLenX.Text, out double thx)) config.OverrideHookLenTopX = thx;
                if (CheckOverrideTopHookY.IsChecked == true && double.TryParse(InputTopHookLenY.Text, out double thy)) config.OverrideHookLenTopY = thy;

                // Additional T1
                config.AddT1Enabled = CheckAdditionalT1.IsChecked == true;
                config.AddT1Type = ComboAddT1Type.SelectedItem as RebarBarType;
                if (int.TryParse(InputAddT1Count.Text, out int at1)) config.AddT1Count = at1;

                // Additional T2
                config.AddT2Enabled = CheckAdditionalT2.IsChecked == true;
                config.AddT2Type = ComboAddT2Type.SelectedItem as RebarBarType;
                if (int.TryParse(InputAddT2Count.Text, out int at2)) config.AddT2Count = at2;

                // Dowels & Stirrups
                config.DowelsEnabled = CheckAddDowels.IsChecked == true;
                config.DowelBarType = ComboDowelBarType.SelectedItem as RebarBarType;
                config.DowelHookBase = ComboDowelHookBase.SelectedItem as RebarHookType;
                if (int.TryParse(InputDowelCount.Text, out int dc)) config.DowelCount = dc;
                if (double.TryParse(InputDowelLength.Text, out double dl)) config.DowelLength = dl;
                
                config.StirrupsEnabled = CheckAddStirrups.IsChecked == true;
                config.StirrupBarType = ComboStirrupBarType.SelectedItem as RebarBarType;
                if (double.TryParse(InputStirrupSpacing.Text, out double ss)) config.StirrupSpacing = ss;

                // Selection
                List<Element> foundations = new List<Element>();
                var selectedIds = _uiDoc.Selection.GetElementIds();
                foreach (var id in selectedIds)
                {
                    Element elem = _doc.GetElement(id);
                    if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation) 
                        foundations.Add(elem);
                }

                if (foundations.Count == 0)
                {
                    TaskDialog.Show("Revit AI Agent", "Please select at least one Isolated Footing.");
                    return;
                }

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
