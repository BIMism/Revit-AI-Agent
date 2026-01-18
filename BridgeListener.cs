using System;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace RevitAIAgent
{
    public class BridgeListener
    {
        private FileSystemWatcher _watcher;
        private ExternalEvent _exEvent;
        private RevitRequestHandler _handler;
        private string _bridgeFile = @"C:\Temp\BIMism_Bridge.json";

        public BridgeListener(ExternalEvent exEvent, RevitRequestHandler handler)
        {
            _exEvent = exEvent;
            _handler = handler;
            StartListening();
        }

        private void StartListening()
        {
            try
            {
                string dir = Path.GetDirectoryName(_bridgeFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Create initial file if not exists
                if (!File.Exists(_bridgeFile))
                {
                    File.WriteAllText(_bridgeFile, JsonConvert.SerializeObject(new BridgeCommand { Status = "IDLE" }));
                }

                _watcher = new FileSystemWatcher(dir, Path.GetFileName(_bridgeFile));
                _watcher.NotifyFilter = NotifyFilters.LastWrite;
                _watcher.Changed += OnFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                // Log silently or show debug
                System.Diagnostics.Debug.WriteLine("Bridge Init Failed: " + ex.Message);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Give file time to close
            Thread.Sleep(100);

            try
            {
                string json = "";
                using (var fs = new FileStream(_bridgeFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    json = sr.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(json)) return;

                var cmd = JsonConvert.DeserializeObject<BridgeCommand>(json);

                if (cmd != null && cmd.Status == "PENDING")
                {
                    // Trigger Revit Main Thread
                    _handler.Request = RequestId.ExecuteScript;
                    _handler.StringParam = cmd.Code;
                    _exEvent.Raise();
                }
            }
            catch (Exception ex)
            {
                // File lock issues common with watchers
                System.Diagnostics.Debug.WriteLine("Bridge Read Failed: " + ex.Message);
            }
        }

        public class BridgeCommand
        {
            public string CommandId { get; set; }
            public string Action { get; set; }
            public string Code { get; set; }
            public string Status { get; set; } // PENDING, SUCCESS, ERROR
            public string Result { get; set; }
        }
    }
}
