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
        private double _footingW = 2000; // in mm
        private double _footingL = 2000; 
        private double _footingH = 600;
        private FoundationType _currentType = FoundationType.Isolated;

        public enum FoundationType
        {
            Isolated,
            Strip,
            Pile
        }

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

            // Hook Orientations
            var hookOrients = new List<string> { "Top - Top", "Bottom - Bottom", "Top - Bottom", "Bottom - Top" };
            
            ComboHookOrientX.ItemsSource = hookOrients; ComboHookOrientX.SelectedIndex = 0; // Top-Top default for Bottom
            ComboHookOrientY.ItemsSource = hookOrients; ComboHookOrientY.SelectedIndex = 0; 
            ComboTopHookXOrient.ItemsSource = hookOrients; ComboTopHookXOrient.SelectedIndex = 1; // Bottom-Bottom default for Top
            ComboTopHookYOrient.ItemsSource = hookOrients; ComboTopHookYOrient.SelectedIndex = 1;

            // --- Parametric Dimension Initialization ---
            try
            {
                var selectedIds = _uiDoc.Selection.GetElementIds();
                if (selectedIds.Count > 0)
                {
                    Element elem = _doc.GetElement(selectedIds.First());
                    if (elem != null && (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation))
                    {
                        double w = GetParamValue(elem, "Width", 2.0);
                        double l = GetParamValue(elem, "Length", 2.0);
                        double h = GetParamValue(elem, "Thickness", 0.6);
                        if (h == 0.6) h = GetParamValue(elem, "Foundation Thickness", 0.6);

                        _footingW = UnitUtils.ConvertFromInternalUnits(w, UnitTypeId.Millimeters);
                        _footingL = UnitUtils.ConvertFromInternalUnits(l, UnitTypeId.Millimeters);
                        _footingH = UnitUtils.ConvertFromInternalUnits(h, UnitTypeId.Millimeters);
                    }
                }
            }
            catch { }
        }

        private double GetParamValue(Element e, string name, double def)
        {
            Parameter p = e.LookupParameter(name);
            if (p != null && p.HasValue) return p.AsDouble();
            
            // Try type parameter
            ElementId typeId = e.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                Element type = e.Document.GetElement(typeId);
                p = type.LookupParameter(name);
                if (p != null && p.HasValue) return p.AsDouble();
            }
            return def;
        }

        private void BtnIsolated_Click(object sender, RoutedEventArgs e)
        {
            _currentType = FoundationType.Isolated;
            SetupUIForType();
        }

        private void BtnStrip_Click(object sender, RoutedEventArgs e)
        {
            _currentType = FoundationType.Strip;
            SetupUIForType();
        }

        private void BtnPile_Click(object sender, RoutedEventArgs e)
        {
            _currentType = FoundationType.Pile;
            SetupUIForType();
        }

        private void SetupUIForType()
        {
            TypeSelectionGrid.Visibility = System.Windows.Visibility.Collapsed;
            DetailGrid.Visibility = System.Windows.Visibility.Visible;

            SidebarList.Items.Clear();
            SidebarList.Items.Add(new ListBoxItem { Content = "Geometry", Padding = new Thickness(10), IsSelected = true });

            switch (_currentType)
            {
                case FoundationType.Isolated:
                    TitleText.Text = "Isolated Footing Reinforcement";
                    SidebarList.Items.Add(new ListBoxItem { Content = "Bottom Major (B1)", Padding = new Thickness(10) });
                    SidebarList.Items.Add(new ListBoxItem { Content = "Bottom Minor (B2)", Padding = new Thickness(10) });
                    SidebarList.Items.Add(new ListBoxItem { Content = "Top Major (T1)", Padding = new Thickness(10) });
                    SidebarList.Items.Add(new ListBoxItem { Content = "Top Minor (T2)", Padding = new Thickness(10) });
                    break;
                case FoundationType.Strip:
                    TitleText.Text = "Strip/Wall Footing Reinforcement";
                    SidebarList.Items.Add(new ListBoxItem { Content = "Longitudinal Bars", Padding = new Thickness(10) });
                    SidebarList.Items.Add(new ListBoxItem { Content = "Stirrups", Padding = new Thickness(10) });
                    break;
                case FoundationType.Pile:
                    TitleText.Text = "Pile Reinforcement";
                    SidebarList.Items.Add(new ListBoxItem { Content = "Vertical Bars", Padding = new Thickness(10) });
                    SidebarList.Items.Add(new ListBoxItem { Content = "Ties / Spirals", Padding = new Thickness(10) });
                    break;
            }
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
            // Show selected
            int index = SidebarList.SelectedIndex;
            
            if (_currentType == FoundationType.Isolated)
            {
                switch (index)
                {
                    case 0: PanelGeometry.Visibility = System.Windows.Visibility.Visible; break;
                    case 1: PanelB1.Visibility = System.Windows.Visibility.Visible; break;
                    case 2: PanelB2.Visibility = System.Windows.Visibility.Visible; break;
                    case 3: PanelT1.Visibility = System.Windows.Visibility.Visible; break;
                    case 4: PanelT2.Visibility = System.Windows.Visibility.Visible; break;
                    // Dowels/Stirrups hidden for pure Isolated for now, can be added back if requested
                }
            }
            else if (_currentType == FoundationType.Strip)
            {
                 switch (index)
                {
                    case 0: PanelGeometry.Visibility = System.Windows.Visibility.Visible; break;
                    case 1: PanelB1.Visibility = System.Windows.Visibility.Visible; break; // Reuse B1 for Longitudinal
                    case 2: PanelStirrups.Visibility = System.Windows.Visibility.Visible; break; 
                }
            }
            else if (_currentType == FoundationType.Pile)
            {
                 switch (index)
                {
                    case 0: PanelGeometry.Visibility = System.Windows.Visibility.Visible; break;
                    case 1: PanelB1.Visibility = System.Windows.Visibility.Visible; break; // Reuse B1 for Vertical
                    case 2: PanelStirrups.Visibility = System.Windows.Visibility.Visible; break; // Reuse Stirrups for Ties
                }
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
            
            // Parametric proportions
            double realW = isMajor ? _footingW : _footingL;
            double realH = _footingH;
            
            // Scaling to fit canvas (max width 240, max height 100)
            double scaleW = 220 / realW;
            double scaleH = 80 / realH;
            double scale = Math.Min(scaleW, scaleH);
            if (scale > 0.5) scale = 0.5; // Don't overscale small things

            double drawW = realW * scale;
            double drawH = realH * scale;
            double x0 = (cw - drawW) / 2, y0 = (ch - drawH) / 2 + 10;

            // Footing Rect
            System.Windows.Shapes.Rectangle footing = new System.Windows.Shapes.Rectangle
            {
                Width = drawW, Height = drawH, Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(242, 242, 242)), 
                Stroke = System.Windows.Media.Brushes.DimGray, StrokeThickness = 2
            };
            Canvas.SetLeft(footing, x0); Canvas.SetTop(footing, y0);
            canvas.Children.Add(footing);

            var redBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 20, 20));
            var greyBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));

            // --- BOTTOM BARS ---
            bool isX = isMajor; 
            string hookName = isX ? ComboHookX?.Text : ComboHookY?.Text;
            
            // Spacing vs Count logic
            double spacing = 200;
            double.TryParse(isX ? InputSpacingX?.Text : InputSpacingY?.Text, out spacing);
            if (spacing <= 0) spacing = 200;
            
            // Calculate how many dots (cross-bars depend on the OTHER dimension)
            double crossDim = isMajor ? _footingL : _footingW;
            double crossSpacing = 200;
            double.TryParse(isMajor ? InputSpacingY?.Text : InputSpacingX?.Text, out crossSpacing);
            if (crossSpacing <= 0) crossSpacing = 200;
            
            int mainDots = (int)(crossDim / crossSpacing) + 1;
            if (mainDots < 2) mainDots = 2;
            if (mainDots > 20) mainDots = 20; // Cap for visual clarity

            // Additional dots?
            bool hasAddDots = isMajor ? (CheckAdditionalB2?.IsChecked == true) : (CheckAdditionalB1?.IsChecked == true);
            int addDots = 0;
            if (hasAddDots) int.TryParse(isMajor ? InputAddB2Count?.Text : InputAddB1Count?.Text, out addDots);
            int totalDots = mainDots + addDots;

            // Main Line with Hook Logic (improved visualization)
            double hookSize = Math.Max(20, drawH * 0.3); // Hook proportional to height
            double offset = 10 + (isMajor ? 0 : 6); // Layer separation

            PointCollection bPts = new PointCollection();
            if (hookName != null && hookName.Contains("None")) {
                // Straight bar, no hooks
                bPts.Add(new System.Windows.Point(x0+10, y0+drawH-offset));
                bPts.Add(new System.Windows.Point(x0+drawW-10, y0+drawH-offset));
            } else if (hookName != null && hookName.Contains("180")) {
                // 180 degree hook (closed loop)
                double loopW = 12;
                bPts.Add(new System.Windows.Point(x0+10+loopW, y0+drawH-offset-8));
                bPts.Add(new System.Windows.Point(x0+10, y0+drawH-offset-8));
                bPts.Add(new System.Windows.Point(x0+10, y0+drawH-offset));
                bPts.Add(new System.Windows.Point(x0+drawW-10, y0+drawH-offset));
                bPts.Add(new System.Windows.Point(x0+drawW-10, y0+drawH-offset-8));
                bPts.Add(new System.Windows.Point(x0+drawW-10-loopW, y0+drawH-offset-8));
            } else { // 90 deg or Standard hook
                // L-shaped hook (most common)
                bPts.Add(new System.Windows.Point(x0+10, y0+drawH-offset-hookSize));
                bPts.Add(new System.Windows.Point(x0+10, y0+drawH-offset));
                bPts.Add(new System.Windows.Point(x0+drawW-10, y0+drawH-offset));
                bPts.Add(new System.Windows.Point(x0+drawW-10, y0+drawH-offset-hookSize));
            }
            canvas.Children.Add(new System.Windows.Shapes.Polyline { Stroke = redBrush, StrokeThickness = 2.5, Points = bPts });

            // Dots (Cross bars)
            double dotZ = y0 + drawH - offset - 4;
            for(int i=0; i < totalDots; i++) {
                System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse { Width=5, Height=5, Fill=redBrush };
                Canvas.SetLeft(dot, x0 + 15 + i * (drawW-30)/(totalDots-1) - 2.5);
                Canvas.SetTop(dot, dotZ - 2.5);
                canvas.Children.Add(dot);
            }

            // --- TOP BARS ---
            if (CheckAddTopBars?.IsChecked == true) {
                double topOffset = 10 + (isMajor ? 0 : 6);
                
                PointCollection tPts = new PointCollection();
                tPts.Add(new System.Windows.Point(x0+10, y0+topOffset+hookSize));
                tPts.Add(new System.Windows.Point(x0+10, y0+topOffset));
                tPts.Add(new System.Windows.Point(x0+drawW-10, y0+topOffset));
                tPts.Add(new System.Windows.Point(x0+drawW-10, y0+topOffset+hookSize));
                canvas.Children.Add(new System.Windows.Shapes.Polyline { Stroke = redBrush, StrokeThickness = 2.5, Points = tPts });

                double tDotZ = y0 + topOffset + 4;
                for(int i=0; i<8; i++) {
                    System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse { Width=5, Height=5, Fill=redBrush };
                    Canvas.SetLeft(dot, x0 + 15 + i * (drawW-30)/7 - 2.5);
                    Canvas.SetTop(dot, tDotZ - 2.5);
                    canvas.Children.Add(dot);
                }
            }


            // Labels for dimensions (rounded to whole numbers)
            int roundedW = (int)Math.Round(realW);
            int roundedH = (int)Math.Round(realH);
            
            TextBlock wLabel = new TextBlock { 
                Text = $"{roundedW} mm", 
                FontSize=9, 
                Foreground=System.Windows.Media.Brushes.Gray 
            };
            Canvas.SetLeft(wLabel, x0 + drawW/2 - 20); 
            Canvas.SetTop(wLabel, y0 + drawH + 2);
            canvas.Children.Add(wLabel);

            TextBlock hLabel = new TextBlock { 
                Text = $"{roundedH} mm", 
                FontSize=9, 
                Foreground=System.Windows.Media.Brushes.Gray 
            };
            Canvas.SetLeft(hLabel, x0 + drawW + 5); 
            Canvas.SetTop(hLabel, y0 + drawH/2 - 5);
            canvas.Children.Add(hLabel);
        }

        private void HookUpEvents()
        {
            // Toggles
            CheckAddTopBars.Click += (s, e) => UpdatePreviews();
            CheckAddDowels.Click += (s, e) => UpdatePreviews();
            CheckAddStirrups.Click += (s, e) => UpdatePreviews();
            CheckAdditionalB1.Click += (s, e) => UpdatePreviews();
            CheckAdditionalB2.Click += (s, e) => UpdatePreviews();
            CheckAdditionalT1.Click += (s, e) => UpdatePreviews();
            CheckAdditionalT2.Click += (s, e) => UpdatePreviews();

            // Inputs
            InputAddB1Count.TextChanged += (s, e) => UpdatePreviews();
            InputAddB2Count.TextChanged += (s, e) => UpdatePreviews();
            InputDowelCount.TextChanged += (s, e) => UpdatePreviews();
            InputSpacingX.TextChanged += (s, e) => UpdatePreviews();
            InputSpacingY.TextChanged += (s, e) => UpdatePreviews();
            
            // Combos
            ComboHookX.SelectionChanged += (s, e) => UpdatePreviews();
            ComboHookY.SelectionChanged += (s, e) => UpdatePreviews();
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
                
                GetHookOrient(ComboHookOrientX.SelectedItem, out var hXStart, out var hXEnd);
                config.B1_HookOrientStart = hXStart; config.B1_HookOrientEnd = hXEnd;

                GetHookOrient(ComboHookOrientY.SelectedItem, out var hYStart, out var hYEnd);
                config.B2_HookOrientStart = hYStart; config.B2_HookOrientEnd = hYEnd;

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

                GetHookOrient(ComboTopHookXOrient.SelectedItem, out var thXStart, out var thXEnd);
                config.T1_HookOrientStart = thXStart; config.T1_HookOrientEnd = thXEnd;

                GetHookOrient(ComboTopHookYOrient.SelectedItem, out var thYStart, out var thYEnd);
                config.T2_HookOrientStart = thYStart; config.T2_HookOrientEnd = thYEnd;

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

                if (_currentType == FoundationType.Isolated)
                {
                    IsolatedRebarGenerator generator = new IsolatedRebarGenerator();
                    generator.Generate(_doc, foundations, config);
                }
                else if (_currentType == FoundationType.Strip)
                {
                    StripRebarGenerator generator = new StripRebarGenerator();
                    generator.Generate(_doc, foundations, config);
                }
                else if (_currentType == FoundationType.Pile)
                {
                    PileRebarGenerator generator = new PileRebarGenerator();
                    generator.Generate(_doc, foundations, config);
                }

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

        private void GetHookOrient(object selectedItem, out RebarHookOrientation start, out RebarHookOrientation end)
        {
            start = RebarHookOrientation.Left; 
            end = RebarHookOrientation.Right;
            
            if (selectedItem is string s)
            {
                 if (s == "Top - Top") { start = RebarHookOrientation.Left; end = RebarHookOrientation.Left; }
                 else if (s == "Bottom - Bottom") { start = RebarHookOrientation.Right; end = RebarHookOrientation.Right; }
                 else if (s == "Top - Bottom") { start = RebarHookOrientation.Left; end = RebarHookOrientation.Right; }
                 else if (s == "Bottom - Top") { start = RebarHookOrientation.Right; end = RebarHookOrientation.Left; }
            }
        }
    }
}
