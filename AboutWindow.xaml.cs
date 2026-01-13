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
                UpdateStatusText.Text = "Downloading update...";

                bool success = await AutoUpdater.DownloadAndInstallAsync(_latestVersion.DownloadUrl);

                if (success)
                {
                    MessageBox.Show(
                        "Update installed successfully!\n\nPlease restart Revit to use the new version.",
                        "Update Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    UpdateStatusText.Text = "❌ Update failed. Please try again.";
                    UpdateNowBtn.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateNowBtn.IsEnabled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
