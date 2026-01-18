using System;
using System.Windows;

namespace RevitAIAgent
{
    public partial class TeachWindow : Window
    {
        private string _originalQuery;

        public TeachWindow(string userQuery)
        {
            InitializeComponent();
            _originalQuery = userQuery;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = CodeInput.Text;
                if (string.IsNullOrWhiteSpace(code) || code.StartsWith("//"))
                {
                    MessageBox.Show("Please enter valid code.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BrainManager.Learn(_originalQuery, code, "User corrected method.");
                MessageBox.Show("Lesson saved! I will remember this next time.", "Brain Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving lesson: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
