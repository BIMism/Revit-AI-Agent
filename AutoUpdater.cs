using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public class AutoUpdater
    {
        private static string _logPath;

        private static void Log(string message)
        {
            try
            {
                if (_logPath == null)
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    _logPath = Path.Combine(appData, "Autodesk", "Revit", "Addins", "2025", "BIMism_Update.log");
                }
                File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n");
            }
            catch { /* Best effort logging */ }
        }

        public static async Task<bool> DownloadAndInstallAsync(string downloadUrl)
        {
            string tempExtract = null;
            string tempZip = null;
            
            try
            {
                Log("Starting update process...");
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                tempZip = Path.Combine(Path.GetTempPath(), $"RevitAIAgent_Update_{timestamp}.zip");
                tempExtract = Path.Combine(Path.GetTempPath(), $"RevitAIAgent_Extract_{timestamp}");
                string addinsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "Autodesk", "Revit", "Addins", "2025");

                // Download ZIP
                Log($"Downloading from {downloadUrl}");
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(2);
                    byte[] zipData = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(tempZip, zipData);
                }
                Log("Download complete.");

                // Extract
                if (Directory.Exists(tempExtract))
                    Directory.Delete(tempExtract, true);
                
                ZipFile.ExtractToDirectory(tempZip, tempExtract, true); // true = overwrite
                Log($"Extracted to {tempExtract}");

                // Handle nested folder if exists (e.g. zip contains "RevitAIAgent" folder at root)
                string sourceRoot = tempExtract;
                var subDirs = Directory.GetDirectories(tempExtract);
                var files = Directory.GetFiles(tempExtract);
                
                // If there's only one directory and no files, assume nested structure
                if (subDirs.Length == 1 && files.Length == 0)
                {
                    sourceRoot = subDirs[0];
                    Log($"Detected nested structure. Root adjusted to: {sourceRoot}");
                }

                // Copy files to appropriate folders
                // Logic: .addin goes to root, everything else goes to BIMism subfolder
                string rootAddinsFolder = addinsFolder; // %AppData%/.../2025
                string binariesFolder = Path.Combine(addinsFolder, "BIMism"); // %AppData%/.../2025/BIMism

                Log($"Installing to Root: {rootAddinsFolder}");
                Log($"Installing to Binaries: {binariesFolder}");

                if (!Directory.Exists(binariesFolder))
                    Directory.CreateDirectory(binariesFolder);

                // We need to iterate the source files and decide where they go
                foreach (string newPath in Directory.GetFiles(sourceRoot, "*.*", SearchOption.AllDirectories))
                {
                     string fileName = Path.GetFileName(newPath);
                     string targetDir;

                     if (fileName.Equals("BIMism.addin", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".addin", StringComparison.OrdinalIgnoreCase))
                     {
                         targetDir = rootAddinsFolder;
                     }
                     else
                     {
                         // Maintain relative structure within the BIMism folder
                         string relativePathFromSource = Path.GetRelativePath(sourceRoot, newPath);
                         // If the file is in a subdir in source, keep that subdir structure in target
                         string relativeDir = Path.GetDirectoryName(relativePathFromSource);
                         targetDir = Path.Combine(binariesFolder, relativeDir);
                     }

                     if (!Directory.Exists(targetDir))
                         Directory.CreateDirectory(targetDir);

                     string destFile = Path.Combine(targetDir, fileName);
                     
                     // Perform the robust copy
                     CopyFileWithRetry(newPath, destFile);
                }

                Log("Update installed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex}");
                TaskDialog.Show("Update Error", $"Failed to install update:\n{ex.Message}\n\nCheck log at {_logPath}");
                return false;
            }
            finally
            {
                try
                {
                    // Cleanup
                    if (tempExtract != null && Directory.Exists(tempExtract))
                        Directory.Delete(tempExtract, true);
                    if (tempZip != null && File.Exists(tempZip))
                        File.Delete(tempZip);
                }
                catch (Exception cleanupEx) { Log($"Cleanup warning: {cleanupEx.Message}"); }
            }
        }

        private static void CopyFileWithRetry(string sourceFile, string destFile)
        {
            bool copied = false;
            int attempts = 0;
            
            while (!copied && attempts < 3)
            {
                try 
                {
                    File.Copy(sourceFile, destFile, true);
                    copied = true;
                }
                catch (IOException) // File likely in use
                {
                    attempts++;
                    try 
                    {
                        // Try renaming the locked file
                        string oldFile = destFile + ".old";
                        if (File.Exists(oldFile)) File.Delete(oldFile); // clear previous old file
                        
                        File.Move(destFile, oldFile); 
                        File.Copy(sourceFile, destFile, true);
                        copied = true;
                        Log($"Renamed locked file: {Path.GetFileName(destFile)}");
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(500); // Wait and retry
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to copy {Path.GetFileName(destFile)}: {ex.Message}");
                    break; // Non-IO error, stop trying
                }
            }
        }
    }
}
