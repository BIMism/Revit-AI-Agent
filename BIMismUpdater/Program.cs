using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace BIMismUpdater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("BIMism Auto-Updater v1.0");
            Console.WriteLine("------------------------");

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: BIMismUpdater.exe <DownloadUrl> <TargetDir>");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            string downloadUrl = args[0];
            string targetDir = args[1];
            string zipPath = Path.Combine(Path.GetTempPath(), "BIMismUpdate.zip");

            try
            {
                // 1. Wait for Revit to close
                Console.WriteLine("Waiting for Revit to close...");
                WaitForRevitToClose();

                // 2. Download Update
                Console.WriteLine($"Downloading update from: {downloadUrl}");
                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(zipPath, data);
                }
                Console.WriteLine("Download complete.");

                // 3. Extract and Overwrite
                Console.WriteLine($"Installing to: {targetDir}");
                if (Directory.Exists(targetDir))
                {
                    // Backup optional, for now just overwrite
                    ZipFile.ExtractToDirectory(zipPath, targetDir, true);
                    Console.WriteLine("Files updated successfully.");
                }
                else
                {
                    Console.WriteLine($"Target directory not found: {targetDir}");
                    ZipFile.ExtractToDirectory(zipPath, targetDir); 
                }

                Console.WriteLine("✅ UPDATE COMPLETE.");
                Console.WriteLine("You can now open Revit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
            }
            finally
            {
                if (File.Exists(zipPath)) File.Delete(zipPath);
                Console.WriteLine("Press any key to close.");
                Console.ReadKey();
            }
        }

        static void WaitForRevitToClose()
        {
            while (Process.GetProcessesByName("Revit").Length > 0)
            {
                Console.Write(".");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine();
        }
    }
}
