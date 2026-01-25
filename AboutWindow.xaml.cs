using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace RevitAIAgent
{
    public partial class AboutWindow : Window
    {
        private UpdateChecker.VersionInfo _latestVersion;

        public AboutWindow()
        {
            InitializeComponent();
            LoadCurrentVersion();
            CheckForUpdates();
        }

        private void LoadCurrentVersion()
        {
            // Get version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        private async void CheckForUpdates()
        {
            try
            {
                UpdateStatusText.Text = "Checking for updates...";
                UpdateNowBtn.Visibility = Visibility.Collapsed;
                UpdateDetailsText.Visibility = Visibility.Collapsed;

                _latestVersion = await UpdateChecker.CheckForUpdatesAsync();

                if (_latestVersion != null)
                {
                    // Update available
                    UpdateStatusText.Text = $"🎉 New version available: v{_latestVersion.Version}";
                    UpdateStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green

                    UpdateDetailsText.Text = _latestVersion.ReleaseNotes;
                    UpdateDetailsText.Visibility = Visibility.Visible;

                    UpdateNowBtn.Visibility = Visibility.Visible;
                    UpdateNowBtn.IsEnabled = true;
                }
                else
                {
                    // Up to date
                    UpdateStatusText.Text = "✅ You're up to date!";
                    UpdateStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "❌ Could not check for updates. Please check your internet connection.";
                UpdateStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                UpdateDetailsText.Text = ex.Message;
                UpdateDetailsText.Visibility = Visibility.Visible;
            }
        }

        private void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private async void UpdateNowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_latestVersion == null) return;

            try
            {
                UpdateNowBtn.IsEnabled = false;
                UpdateStatusText.Text = "Launching updater...";

                // Get app directory
                string appDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string updaterPath = System.IO.Path.Combine(appDir, "BIMismUpdater.exe");
                // The target directory for extraction should be the parent folder (Revit Addins root)
                // so that the .addin file lands in the root and DLLs land in the /BIMism subfolder.
                string targetDir = System.IO.Path.GetDirectoryName(appDir); 

                if (!System.IO.File.Exists(updaterPath))
                {
                    // If not in the same folder, check the BIMism subfolder (standard install)
                    updaterPath = System.IO.Path.Combine(appDir, "BIMism", "BIMismUpdater.exe");
                    if (!System.IO.File.Exists(updaterPath))
                    {
                        throw new System.IO.FileNotFoundException("Could not find BIMismUpdater.exe. Please install the latest version manually.");
                    }
                }

                // Launch the standalone updater
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"\"{_latestVersion.DownloadUrl}\" \"{targetDir}\"",
                    UseShellExecute = true,
                    Verb = "runas" // Request Admin privileges
                };

                Process.Start(startInfo);

                MessageBox.Show(
                    "The Auto-Updater has been launched.\n\n" +
                    "1. Please CLOSE Revit now.\n" +
                    "2. The updater will then download and install the new version.\n" +
                    "3. You can restart Revit once the updater finishes.",
                    "Update Starting",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.Close();
                // We don't close the main app here, just the About window. 
                // The updater will wait for Revit to close.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed to launch: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateNowBtn.IsEnabled = true;
                UpdateStatusText.Text = "❌ Update failed to launch.";
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
