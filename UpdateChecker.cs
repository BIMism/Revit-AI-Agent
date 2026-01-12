using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RevitAIAgent
{
    public class UpdateChecker
    {
        private const string VERSION_URL = "https://raw.githubusercontent.com/BIMism/Revit-AI-Agent/main/version.json";
        private const string CURRENT_VERSION = "1.7.0";

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
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string json = await client.GetStringAsync(VERSION_URL);
                    VersionInfo latestVersion = JsonConvert.DeserializeObject<VersionInfo>(json);

                    // Compare versions
                    if (IsNewerVersion(latestVersion.Version, CURRENT_VERSION))
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
