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
            
            // Init 3D
            HookUpEvents();
            Update3DPreview();
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
            
            // Trigger 3D update
            Update3DPreview();
        }
        
        private void Update3DPreview()
        {
            if (PreviewViewport == null) return;
            if (CheckDynamicUpdate != null && CheckDynamicUpdate.IsChecked == false) return;

            var toRemove = new List<Visual3D>();
            foreach (var child in PreviewViewport.Children)
            {
                if (!(child is ModelVisual3D mv && mv.Content is Light))
                    toRemove.Add(child as Visual3D);
            }
            foreach (var r in toRemove) PreviewViewport.Children.Remove(r);

            double w = 2.0, l = 2.0, h = 0.8;
            Model3DGroup group = new Model3DGroup();

            // Concrete (Solid light grey for visibility)
            AddCube(group, new Point3D(0, 0, 0), new Point3D(w, l, h), 
                System.Windows.Media.Color.FromArgb(120, 200, 200, 200));

            double cover = 0.12;
            double barThick = 0.1; // Bright red and thick
            var redColor = System.Windows.Media.Color.FromRgb(255, 0, 0);
            var whiteColor = System.Windows.Media.Color.FromRgb(255, 255, 255);

            // B1 & B2 (Bottom Rebar)
            RenderRebarLayer(group, w, l, h, cover, barThick, redColor, true);

            // T1 & T2 (Top Rebar)
            if (CheckAddTopBars?.IsChecked == true)
                RenderRebarLayer(group, w, l, h, cover, barThick, redColor, false);

            // Dowels
            if (CheckAddDowels?.IsChecked == true)
                RenderDowels(group, w, l, h, cover, 0.1, whiteColor);

            ModelVisual3D modelVis = new ModelVisual3D { Content = group };
            PreviewViewport.Children.Add(modelVis);

            UpdateSectionPreview();
        }

        private void UpdateSectionPreview()
        {
            if (SectionCanvas == null) return;
            SectionCanvas.Children.Clear();

            double cw = SectionCanvas.Width, ch = SectionCanvas.Height;
            double fw = 180, fh = 60;
            double x0 = (cw - fw) / 2, y0 = (ch - fh) / 2 + 20;

            // Footing Rect
            System.Windows.Shapes.Rectangle footing = new System.Windows.Shapes.Rectangle
            {
                Width = fw, Height = fh, Fill = System.Windows.Media.Brushes.LightGray, Stroke = System.Windows.Media.Brushes.DimGray, StrokeThickness = 2
            };
            Canvas.SetLeft(footing, x0); Canvas.SetTop(footing, y0);
            SectionCanvas.Children.Add(footing);

            // Bottom Bars
            for(int i=0; i<10; i++)
            {
                System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse { Width=4, Height=4, Fill=System.Windows.Media.Brushes.Red };
                Canvas.SetLeft(dot, x0 + 10 + i * (fw-20)/9 - 2); Canvas.SetTop(dot, y0 + fh - 10 - 2);
                SectionCanvas.Children.Add(dot);
            }
            
            System.Windows.Shapes.Polyline bLine = new System.Windows.Shapes.Polyline { Stroke = System.Windows.Media.Brushes.Red, StrokeThickness = 2, Points = new PointCollection { new System.Windows.Point(x0+5, y0+fh-30), new System.Windows.Point(x0+5, y0+fh-5), new System.Windows.Point(x0+fw-5, y0+fh-5), new System.Windows.Point(x0+fw-5, y0+fh-30) } };
            SectionCanvas.Children.Add(bLine);

            // Top Bars
            if (CheckAddTopBars?.IsChecked == true)
            {
                System.Windows.Shapes.Polyline tLine = new System.Windows.Shapes.Polyline { Stroke = System.Windows.Media.Brushes.Red, StrokeThickness = 2, Points = new PointCollection { new System.Windows.Point(x0+5, y0+30), new System.Windows.Point(x0+5, y0+5), new System.Windows.Point(x0+fw-5, y0+5), new System.Windows.Point(x0+fw-5, y0+30) } };
                SectionCanvas.Children.Add(tLine);
            }

            // Labels
            TextBlock lb = new TextBlock { Text="lb", FontSize=10, Foreground=System.Windows.Media.Brushes.Black };
            Canvas.SetLeft(lb, x0 - 15); Canvas.SetTop(lb, y0 + fh - 20);
            SectionCanvas.Children.Add(lb);
        }

        private void RenderRebarLayer(Model3DGroup group, double w, double l, double h, double cover, double thick, System.Windows.Media.Color c, bool isBottom)
        {
            double z = isBottom ? cover : (h - cover);
            double hookLen = 0.4, hookDir = isBottom ? 1.0 : -1.0;
            int count = 8;
            for (int i = 0; i < count; i++)
            {
                double pos = cover + i * (l - 2 * cover) / (count - 1);
                Point3D p1 = new Point3D(cover, pos, z), p2 = new Point3D(w - cover, pos, z);
                AddCylinder(group, p1, p2, thick, c); 
                AddCylinder(group, p1, new Point3D(p1.X, p1.Y, p1.Z + hookLen * hookDir), thick, c);
                AddCylinder(group, p2, new Point3D(p2.X, p2.Y, p2.Z + hookLen * hookDir), thick, c);
            }
            for (int i = 0; i < count; i++)
            {
                double pos = cover + i * (w - 2 * cover) / (count - 1);
                Point3D p1 = new Point3D(pos, cover, z), p2 = new Point3D(pos, l - cover, z);
                AddCylinder(group, p1, p2, thick, c);
            }
        }

        private void RenderDowels(Model3DGroup group, double w, double l, double h, double cover, double thick, System.Windows.Media.Color c)
        {
            double m = 0.4, dH = h + 0.6;
            AddCylinder(group, new Point3D(m, m, 0), new Point3D(m, m, dH), thick, c);
            AddCylinder(group, new Point3D(w-m, m, 0), new Point3D(w-m, m, dH), thick, c);
            AddCylinder(group, new Point3D(w-m, l-m, 0), new Point3D(w-m, l-m, dH), thick, c);
            AddCylinder(group, new Point3D(m, l-m, 0), new Point3D(m, l-m, dH), thick, c);
        }

        private void AddCube(Model3DGroup group, Point3D min, Point3D max, System.Windows.Media.Color c)
        {
            GeometryModel3D model = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3D[] pts = { new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, max.Y, min.Z), new Point3D(min.X, max.Y, min.Z), new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z), new Point3D(max.X, max.Y, max.Z), new Point3D(min.X, max.Y, max.Z) };
            foreach (var p in pts) mesh.Positions.Add(p);
            int[] idx = { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,1, 1,4,5, 2,6,3, 3,6,7, 1,5,2, 2,5,6, 0,3,4, 3,7,4 };
            foreach (var i in idx) mesh.TriangleIndices.Add(i);
            model.Geometry = mesh;
            model.Material = new DiffuseMaterial(new SolidColorBrush(c));
            group.Children.Add(model);
        }

        private void AddCylinder(Model3DGroup group, Point3D p1, Point3D p2, double rad, System.Windows.Media.Color c)
        {
            double x1 = Math.Min(p1.X, p2.X)-rad, x2 = Math.Max(p1.X, p2.X)+rad, y1 = Math.Min(p1.Y, p2.Y)-rad, y2 = Math.Max(p1.Y, p2.Y)+rad, z1 = Math.Min(p1.Z, p2.Z)-rad, z2 = Math.Max(p1.Z, p2.Z)+rad;
            GeometryModel3D model = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3D[] pts = { new Point3D(x1,y1,z1), new Point3D(x2,y1,z1), new Point3D(x2,y2,z1), new Point3D(x1,y2,z1), new Point3D(x1,y1,z2), new Point3D(x2,y1,z2), new Point3D(x2,y2,z2), new Point3D(x1,y2,z2) };
            foreach (var p in pts) mesh.Positions.Add(p);
            int[] idx = { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,1, 1,4,5, 1,5,2, 2,5,6, 2,6,3, 3,6,7, 3,7,0, 0,7,4 };
            foreach (var i in idx) mesh.TriangleIndices.Add(i);
            model.Geometry = mesh;
            model.Material = new EmissiveMaterial(new SolidColorBrush(c)); 
            group.Children.Add(model);
        }

        private void HookUpEvents()
        {
            // All toggle events
            CheckAddTopBars.Checked += (s, e) => Update3DPreview();
            CheckAddTopBars.Unchecked += (s, e) => Update3DPreview();
            CheckAddDowels.Checked += (s, e) => Update3DPreview();
            CheckAddDowels.Unchecked += (s, e) => Update3DPreview();
            CheckAdditionalB1.Checked += (s, e) => Update3DPreview();
            CheckAdditionalB1.Unchecked += (s, e) => Update3DPreview();
            CheckAdditionalB2.Checked += (s, e) => Update3DPreview();
            CheckAdditionalB2.Unchecked += (s, e) => Update3DPreview();
            CheckAdditionalT1.Checked += (s, e) => Update3DPreview();
            CheckAdditionalT1.Unchecked += (s, e) => Update3DPreview();
            CheckAdditionalT2.Checked += (s, e) => Update3DPreview();
            CheckAdditionalT2.Unchecked += (s, e) => Update3DPreview();

            // All input change events
            InputAddB1Count.TextChanged += (s, e) => Update3DPreview();
            InputAddB2Count.TextChanged += (s, e) => Update3DPreview();
            InputDowelCount.TextChanged += (s, e) => Update3DPreview();
            InputSpacingX.TextChanged += (s, e) => Update3DPreview();
            InputSpacingY.TextChanged += (s, e) => Update3DPreview();
            
            // Selection events
            SidebarList.SelectionChanged += (s, e) => Update3DPreview();
            ComboBarTypeX.SelectionChanged += (s, e) => Update3DPreview();
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
