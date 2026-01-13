using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;

namespace BIMismInstaller
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string addinsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk", "Revit", "Addins", "2025");

                MessageBox.Show("Welcome to the BIM'ism AI Agent Installer!\n\nThis will install the AI Agent plugin for Revit 2025.", "BIM'ism Installer", MessageBoxButton.OK, MessageBoxImage.Information);

                if (!Directory.Exists(addinsFolder))
                {
                    Directory.CreateDirectory(addinsFolder);
                }

                // Extract embedded ZIP
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("RevitAIAgent.zip")!)
                {
                    if (stream == null)
                    {
                        MessageBox.Show("Error: Installation files not found inside the installer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string tempZip = Path.Combine(Path.GetTempPath(), "target_plugin.zip");
                    using (FileStream fileStream = new FileStream(tempZip, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }

                    // Extract to addins folder
                    using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // Determine target folder based on file type
                            string targetBaseFolder = addinsFolder;
                            
                            // If it's NOT a .addin file, put it in the "BIMism" subfolder
                            if (!entry.Name.EndsWith(".addin", StringComparison.OrdinalIgnoreCase))
                            {
                                targetBaseFolder = Path.Combine(addinsFolder, "BIMism");
                            }

                            string destinationPath = Path.Combine(targetBaseFolder, entry.FullName);
                            string? directoryPath = Path.GetDirectoryName(destinationPath);

                            if (directoryPath != null && !Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }
                            
                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                // Overwrite existing files
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }

                    File.Delete(tempZip);
                }

                MessageBox.Show("Installation Successful!\n\nBIM'ism AI Agent has been installed to your Revit 2025 Add-ins folder.\n\nPlease restart Revit to start using the tool.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
