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

            // Clear existing models except lights
            var toRemove = new List<Visual3D>();
            foreach (var child in PreviewViewport.Children)
            {
                // Simple heuristic: keep lights, remove GeometryModel3D wrapper
                if (!(child is ModelVisual3D mv && mv.Content is Light))
                {
                    toRemove.Add(child as Visual3D);
                }
            }
            foreach (var r in toRemove) PreviewViewport.Children.Remove(r);

            // Create Footing Box (Transparent Grey)
            // Hardcoded preview size for now, ideally binding to geometry inputs
            double w = 2.0; double l = 2.0; double h = 1.0;
            
            Model3DGroup group = new Model3DGroup();
            
            // Concrete
            GeometryModel3D concrete = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();
            
            // Vertices 
            Point3D[] corners = new Point3D[] {
                new Point3D(0,0,0), new Point3D(w,0,0), new Point3D(w,l,0), new Point3D(0,l,0),
                new Point3D(0,0,h), new Point3D(w,0,h), new Point3D(w,l,h), new Point3D(0,l,h)
            };
            foreach(var p in corners) mesh.Positions.Add(p);
            
            // Indices (Triangles)
            int[] indices = new int[] { 
                0,1,2, 0,2,3, // Bottom
                4,6,5, 4,7,6, // Top
                0,4,1, 1,4,5, // Front
                2,6,3, 3,6,7, // Back
                1,5,2, 2,5,6, // Right
                0,3,4, 3,7,4  // Left
            };
            foreach(var i in indices) mesh.TriangleIndices.Add(i);
            
            concrete.Geometry = mesh;
            // Transparent Gray
            concrete.Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 200, 200, 200))); 
            // Also add back material for inside
            concrete.BackMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 150, 150, 150)));
            group.Children.Add(concrete);

            // Rebar (Approximated as thin boxes/lines)
            // B1 (X dir, bottom)
            AddRebarSet(group, w, l, h, true, true, 8, System.Windows.Media.Color.FromRgb(255,0,0));
            // B2 (Y dir, bottom)
            AddRebarSet(group, w, l, h, false, true, 8, System.Windows.Media.Color.FromRgb(0,255,0));
            
            if (CheckAddTopBars.IsChecked == true)
            {
                 // T1 (X dir, top)
                AddRebarSet(group, w, l, h, true, false, 8, System.Windows.Media.Color.FromRgb(255,50,50));
                // T2 (Y dir, top)
                AddRebarSet(group, w, l, h, false, false, 8, System.Windows.Media.Color.FromRgb(50,255,50));
            }

            ModelVisual3D modelVis = new ModelVisual3D();
            modelVis.Content = group;
            PreviewViewport.Children.Add(modelVis);
        }

        private void AddRebarSet(Model3DGroup group, double w, double l, double h, bool isX, bool isBottom, int count, System.Windows.Media.Color c)
        {
            double cover = 0.1;
            double z = isBottom ? cover : h - cover;
            double spacing = isX ? (l - 2*cover) / (count-1) : (w - 2*cover) / (count-1);
            
            for(int i=0; i<count; i++)
            {
                GeometryModel3D bar = new GeometryModel3D();
                MeshGeometry3D mesh = new MeshGeometry3D();
                
                double thickness = 0.02;
                Point3D p1 = isX 
                    ? new Point3D(cover, cover + i*spacing, z)
                    : new Point3D(cover + i*spacing, cover, z);
                
                Point3D p2 = isX
                    ? new Point3D(w - cover, cover + i*spacing, z)
                    : new Point3D(cover + i*spacing, l - cover, z);
                
                // Extremely simple line thickness (box)
                mesh.Positions.Add(p1);
                mesh.Positions.Add(p2);
                mesh.Positions.Add(new Point3D(p1.X, p1.Y, p1.Z+thickness));
                mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2);
                
                bar.Geometry = mesh;
                bar.Material = new DiffuseMaterial(new SolidColorBrush(c));
                group.Children.Add(bar);
            }
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
