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
        private DispatcherTimer _timer;
        private ExternalEvent _exEvent;
        private RevitRequestHandler _handler;
        private string _bridgeFile = @"C:\Temp\BIMism_Bridge.json";
        private DateTime _lastModified = DateTime.MinValue;

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

                // Check every 0.5 seconds (High frequency polling)
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(500);
                _timer.Tick += OnTimerTick;
                _timer.Start();
            }
            catch (Exception ex)
            {
                 // Silent fail
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(_bridgeFile)) return;

                // Check if file modified
                DateTime currentModified = File.GetLastWriteTime(_bridgeFile);
                if (currentModified <= _lastModified) return;

                _lastModified = currentModified;
                ProcessFile();
            }
            catch { /* Ignore lock errors */ }
        }

        private void ProcessFile()
        {
             try
            {
                string json = "";
                // Retry loop for file locks
                for(int i=0; i<3; i++)
                {
                    try 
                    {
                        using (var fs = new FileStream(_bridgeFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs))
                        {
                            json = sr.ReadToEnd();
                        }
                        break;
                    }
                    catch { Thread.Sleep(50); }
                }

                if (string.IsNullOrWhiteSpace(json)) return;

                var cmd = JsonConvert.DeserializeObject<BridgeCommand>(json);

                if (cmd != null && cmd.Status == "PENDING")
                {
                    // Trigger Revit Main Thread
                    _handler.Request = RequestId.ExecuteScript;
                    _handler.StringParam = cmd.Code;
                    _handler.CurrentCommandId = cmd.CommandId; // Pass ID for tracking
                    _exEvent.Raise();
                }
            }
            catch { }
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
