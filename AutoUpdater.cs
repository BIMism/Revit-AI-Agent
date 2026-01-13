using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public class AutoUpdater
    {
        public static async Task<bool> DownloadAndInstallAsync(string downloadUrl)
        {
            string tempExtract = null;
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string tempZip = Path.Combine(Path.GetTempPath(), $"RevitAIAgent_Update_{timestamp}.zip");
                tempExtract = Path.Combine(Path.GetTempPath(), $"RevitAIAgent_Extract_{timestamp}");
                string addinsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "Autodesk", "Revit", "Addins", "2025");

                // Download ZIP
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(2);
                    byte[] zipData = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(tempZip, zipData);
                }

                // Extract
                if (Directory.Exists(tempExtract))
                    Directory.Delete(tempExtract, true);
                
                ZipFile.ExtractToDirectory(tempZip, tempExtract);

                // Copy files to Addins folder with Locked File handling
                CopyFilesRecursively(tempExtract, addinsFolder);

                // Cleanup ZIP
                if (File.Exists(tempZip)) File.Delete(tempZip);

                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Update Error", $"Failed to install update:\n{ex.Message}\n\nTry closing Revit and using the manual installer.");
                return false;
            }
            finally
            {
                try
                {
                    if (tempExtract != null && Directory.Exists(tempExtract))
                        Directory.Delete(tempExtract, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            // Create all directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            // Copy all files
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string destFile = newPath.Replace(sourcePath, targetPath);
                
                try 
                {
                    File.Copy(newPath, destFile, true);
                }
                catch (IOException) // File likely in use
                {
                    try 
                    {
                        // Use a unique name for the old file to avoid "Already Exists" collisions
                        string oldFile = destFile + "." + DateTime.Now.Ticks + ".old";
                        File.Move(destFile, oldFile); // Rename existing to unique .old
                        File.Copy(newPath, destFile, true); // Copy new one
                    }
                    catch
                    {
                        // If everything fails, skip this file (it will require a manual install)
                    }
                }
            }
        }
    }
}
