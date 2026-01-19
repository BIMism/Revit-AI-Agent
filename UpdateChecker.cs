using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RevitAIAgent
{
    public class UpdateChecker
    {
        private const string VERSION_URL = "https://raw.githubusercontent.com/BIMism/Revit-AI-Agent/main/version.json";
        
        public class VersionInfo
        {
            [JsonProperty("version")]
            public string Version { get; set; }
            
            [JsonProperty("downloadUrl")]
            public string DownloadUrl { get; set; }
            
            [JsonProperty("releaseNotes")]
            public string ReleaseNotes { get; set; }
        }

        public static async Task<VersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                // Get current version dynamically from the assembly
                string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string json = await client.GetStringAsync(VERSION_URL);
                    VersionInfo latestVersion = JsonConvert.DeserializeObject<VersionInfo>(json);

                    // --- TEST MODE FOR DEMO ---
                    // Since we don't have the real git hosted yet, we mock a newer version to show the UI works.
                    if (true) 
                    {
                        return new VersionInfo 
                        { 
                            Version = "4.0.0", 
                            DownloadUrl = "https://github.com/BIMism/Revit-AI-Agent/archive/refs/heads/main.zip", // Placeholder
                            ReleaseNotes = "Auto-Update System Verified! (This is a test)" 
                        };
                    }
                    // --------------------------

                    // Compare versions
                    if (IsNewerVersion(latestVersion.Version, currentVersion))
                    {
                        return latestVersion;
                    }
                }
            }
            catch
            {
                // Silently fail if no internet or GitHub unavailable
            }

            return null;
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                Version latest = new Version(latestVersion);
                Version current = new Version(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }
    }
}
