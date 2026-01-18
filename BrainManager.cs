using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RevitAIAgent
{
    public class BrainManager
    {
        private static string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BIMism", "RevitAI");
        private static string _userBrainPath = Path.Combine(_appDataPath, "UserBrain.json");
        private static string _masterBrainPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Assets", "RevitKnowledge.json");

        public static List<KnowledgeEntry> UserKnowledge { get; private set; } = new List<KnowledgeEntry>();
        public static List<KnowledgeEntry> MasterKnowledge { get; private set; } = new List<KnowledgeEntry>();

        public class KnowledgeEntry
        {
            public List<string> Keywords { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
        }

        public static void Initialize()
        {
            // Ensure directory exists
            if (!Directory.Exists(_appDataPath)) Directory.CreateDirectory(_appDataPath);

            // Load User Brain
            if (File.Exists(_userBrainPath))
            {
                try
                {
                    string json = File.ReadAllText(_userBrainPath);
                    UserKnowledge = JsonConvert.DeserializeObject<List<KnowledgeEntry>>(json) ?? new List<KnowledgeEntry>();
                }
                catch { /* Ignore corrupt file */ }
            }

            // Load Master Brain (Deployed with app)
            if (File.Exists(_masterBrainPath))
            {
                try
                {
                    string json = File.ReadAllText(_masterBrainPath);
                    MasterKnowledge = JsonConvert.DeserializeObject<List<KnowledgeEntry>>(json) ?? new List<KnowledgeEntry>();
                }
                catch { /* Ignore */ }
            }
        }

        public static KnowledgeEntry Search(string userQuery)
        {
            string query = userQuery.ToLower();

            // 1. Check User Knowledge (Priority)
            var userMatch = UserKnowledge.FirstOrDefault(k => k.Keywords.All(w => query.Contains(w.ToLower())));
            if (userMatch != null) return userMatch;

            // 2. Check Master Knowledge
            var masterMatch = MasterKnowledge.FirstOrDefault(k => k.Keywords.All(w => query.Contains(w.ToLower())));
            if (masterMatch != null) return masterMatch;

            return null;
        }

        public static void Learn(string userQuery, string correctCode, string description)
        {
            // Simple keyword extraction: Split by space, ignore small words
            var keywords = userQuery.Split(' ')
                .Where(w => w.Length > 3)
                .Select(w => w.ToLower())
                .ToList();

            var newEntry = new KnowledgeEntry
            {
                Keywords = keywords,
                Code = correctCode,
                Description = description
            };

            UserKnowledge.Add(newEntry);
            SaveUserBrain();
        }

        private static void SaveUserBrain()
        {
            try
            {
                string json = JsonConvert.SerializeObject(UserKnowledge, Formatting.Indented);
                File.WriteAllText(_userBrainPath, json);
            }
            catch { /* Ignore save errors */ }
        }
    }
}
