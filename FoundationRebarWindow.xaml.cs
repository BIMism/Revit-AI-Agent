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

            var toRemove = new List<Visual3D>();
            foreach (var child in PreviewViewport.Children)
            {
                if (!(child is ModelVisual3D mv && mv.Content is Light))
                    toRemove.Add(child as Visual3D);
            }
            foreach (var r in toRemove) PreviewViewport.Children.Remove(r);

            double w = 2.0, l = 2.0, h = 1.0;
            Model3DGroup group = new Model3DGroup();

            // Concrete (Semi-transparent grey)
            AddCube(group, new Point3D(0, 0, 0), new Point3D(w, l, h), 
                System.Windows.Media.Color.FromArgb(80, 180, 180, 180));

            double cover = 0.1;
            double barThick = 0.08; // MUCH thicker for visibility
            var redColor = System.Windows.Media.Color.FromRgb(220, 50, 50);
            var greyColor = System.Windows.Media.Color.FromRgb(140, 140, 140);

            // B1 & B2 (Bottom Rebar)
            RenderRebarLayer(group, w, l, h, cover, barThick, redColor, true);

            // T1 & T2 (Top Rebar) - if enabled
            if (CheckAddTopBars != null && CheckAddTopBars.IsChecked == true)
                RenderRebarLayer(group, w, l, h, cover, barThick, redColor, false);

            // Dowels (if enabled)
            if (CheckAddDowels != null && CheckAddDowels.IsChecked == true)
            {
                int dowelCount = 4;
                if (int.TryParse(InputDowelCount?.Text, out int dc)) dowelCount = dc;
                RenderDowels(group, w, l, h, cover, dowelCount, barThick, greyColor);
            }

            ModelVisual3D modelVis = new ModelVisual3D { Content = group };
            PreviewViewport.Children.Add(modelVis);
        }

        private void RenderRebarLayer(Model3DGroup group, double w, double l, double h, double cover, double thick, System.Windows.Media.Color c, bool isBottom)
        {
            double z = isBottom ? cover : (h - cover);
            double hookLen = 0.5; // Hook length
            double hookDir = isBottom ? 1.0 : -1.0;
            int count = 8;

            // X-direction bars (B1 or T1)
            for (int i = 0; i < count; i++)
            {
                double y = cover + i * (l - 2 * cover) / (count - 1);
                Point3D p1 = new Point3D(cover, y, z);
                Point3D p2 = new Point3D(w - cover, y, z);
                
                // Main bar
                AddCylinder(group, p1, p2, thick, c);
                
                // Hooks at ends
                AddCylinder(group, p1, new Point3D(p1.X, p1.Y, p1.Z + hookLen * hookDir), thick, c);
                AddCylinder(group, p2, new Point3D(p2.X, p2.Y, p2.Z + hookLen * hookDir), thick, c);
            }

            // Y-direction bars (B2 or T2)
            for (int i = 0; i < count; i++)
            {
                double x = cover + i * (w - 2 * cover) / (count - 1);
                Point3D p1 = new Point3D(x, cover, z);
                Point3D p2 = new Point3D(x, l - cover, z);
                
                // Main bar
                AddCylinder(group, p1, p2, thick, c);
                
                // Hooks at ends
                AddCylinder(group, p1, new Point3D(p1.X, p1.Y, p1.Z + hookLen * hookDir), thick, c);
                AddCylinder(group, p2, new Point3D(p2.X, p2.Y, p2.Z + hookLen * hookDir), thick, c);
            }
        }

        private void RenderDowels(Model3DGroup group, double w, double l, double h, double cover, int count, double thick, System.Windows.Media.Color c)
        {
            double dowelH = h + 0.8; // Extend above footing
            
            if (count == 4)
            {
                // Four corners
                double margin = 0.3;
                AddCylinder(group, new Point3D(margin, margin, 0), new Point3D(margin, margin, dowelH), thick, c);
                AddCylinder(group, new Point3D(w - margin, margin, 0), new Point3D(w - margin, margin, dowelH), thick, c);
                AddCylinder(group, new Point3D(w - margin, l - margin, 0), new Point3D(w - margin, l - margin, dowelH), thick, c);
                AddCylinder(group, new Point3D(margin, l - margin, 0), new Point3D(margin, l - margin, dowelH), thick, c);
            }
            else
            {
                // Distribute evenly
                double spacing = Math.Min(w, l) / (count + 1);
                for (int i = 0; i < count; i++)
                {
                    double pos = spacing * (i + 1);
                    AddCylinder(group, new Point3D(pos, l / 2, 0), new Point3D(pos, l / 2, dowelH), thick, c);
                }
            }
        }

        private void AddCube(Model3DGroup group, Point3D min, Point3D max, System.Windows.Media.Color c)
        {
            GeometryModel3D model = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            Point3D[] corners = {
                new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z),
                new Point3D(max.X, max.Y, min.Z), new Point3D(min.X, max.Y, min.Z),
                new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z),
                new Point3D(max.X, max.Y, max.Z), new Point3D(min.X, max.Y, max.Z)
            };
            foreach (var p in corners) mesh.Positions.Add(p);

            int[] indices = { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,1, 1,4,5, 2,6,3, 3,6,7, 1,5,2, 2,5,6, 0,3,4, 3,7,4 };
            foreach (var i in indices) mesh.TriangleIndices.Add(i);

            model.Geometry = mesh;
            model.Material = new DiffuseMaterial(new SolidColorBrush(c));
            model.BackMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(c.A / 2), c.R, c.G, c.B)));
            group.Children.Add(model);
        }

        private void AddCylinder(Model3DGroup group, Point3D p1, Point3D p2, double radius, System.Windows.Media.Color c)
        {
            // Simplified: render as thick box instead of cylinder for performance
            Vector3D dir = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            double len = dir.Length;
            if (len < 0.001) return;

            double x1 = Math.Min(p1.X, p2.X) - radius;
            double x2 = Math.Max(p1.X, p2.X) + radius;
            double y1 = Math.Min(p1.Y, p2.Y) - radius;
            double y2 = Math.Max(p1.Y, p2.Y) + radius;
            double z1 = Math.Min(p1.Z, p2.Z) - radius;
            double z2 = Math.Max(p1.Z, p2.Z) + radius;

            GeometryModel3D model = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            Point3D[] pts = {
                new Point3D(x1, y1, z1), new Point3D(x2, y1, z1), new Point3D(x2, y2, z1), new Point3D(x1, y2, z1),
                new Point3D(x1, y1, z2), new Point3D(x2, y1, z2), new Point3D(x2, y2, z2), new Point3D(x1, y2, z2)
            };
            foreach (var p in pts) mesh.Positions.Add(p);

            int[] idx = { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,1, 1,4,5, 1,5,2, 2,5,6, 2,6,3, 3,6,7, 3,7,0, 0,7,4 };
            foreach (var i in idx) mesh.TriangleIndices.Add(i);

            model.Geometry = mesh;
            model.Material = new DiffuseMaterial(new SolidColorBrush(c));
            group.Children.Add(model);
        }

        private void HookUpEvents()
        {
            // Helper to hook up events safely
            CheckAddTopBars.Checked += (s, e) => Update3DPreview();
            CheckAddTopBars.Unchecked += (s, e) => Update3DPreview();
            
            CheckAdditionalB1.Checked += (s, e) => Update3DPreview();
            CheckAdditionalB1.Unchecked += (s, e) => Update3DPreview();
   
            CheckAdditionalB2.Checked += (s, e) => Update3DPreview();
            CheckAdditionalB2.Unchecked += (s, e) => Update3DPreview();

            CheckAdditionalT1.Checked += (s, e) => Update3DPreview();
            CheckAdditionalT1.Unchecked += (s, e) => Update3DPreview();

            // Text changes (simple lost focus or changed)
            InputAddB1Count.TextChanged += (s, e) => Update3DPreview();
            InputAddB2Count.TextChanged += (s, e) => Update3DPreview();
            // ... add others if needed
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
