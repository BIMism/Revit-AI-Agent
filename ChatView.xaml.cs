using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.UI;

namespace RevitAIAgent
{
    public partial class ChatView : Page, IDockablePaneProvider
    {
        private AIEngine _ai;
        private ExternalEvent _exEvent;
        private RevitRequestHandler _handler;

        public ChatView(ExternalEvent exEvent, RevitRequestHandler handler)
        {
            InitializeComponent();
            _exEvent = exEvent;
            _handler = handler;
            _ai = new AIEngine();
            
            // Fetch project context on startup
            _handler.Request = RequestId.GetContext;
            _exEvent.Raise();

            AddMessage("AI", "Hello! I am ready to help. I've analyzed your project context.");
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this as FrameworkElement;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }

        public async void RunCommand(string prompt)
        {
            // Allow Ribbon Buttons to drive the AI
            InputBox.Text = prompt;
            await ProcessInput();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessInput();
        }

        private string _lastCodeFromAI = "";

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ProcessInput();
            }
        }

        private async Task ProcessInput()
        {
            string userText = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // 1. Show User Message
            AddMessage("You", userText);
            InputBox.Text = "";

            string lowerText = userText.ToLower();

            // 1. FAST PATH: Instant commands
            if (lowerText == "hi" || lowerText == "hello" || lowerText == "hey" || lowerText == "help" || 
                lowerText.Contains("how are you") || lowerText.Contains("who are you") || 
                lowerText.Contains("what can you do") || lowerText.Contains("what you can do"))
            {
                AddMessage("AI", "I am the Revit AI Agent. I can automate tasks for you. Try 'Select all walls' or 'Create 4 walls'.");
                return;
            }

            // 2. SMART FAST PATH: "Do it" or "Go"
            if ((lowerText == "do it" || lowerText == "go" || lowerText == "okay" || lowerText == "do it you") && !string.IsNullOrEmpty(_lastCodeFromAI))
            {
                AddMessage("AI", "Executing the task now...");
                AddMessage("System", "🛠️ Executing Revit Command...");
                _handler.Request = RequestId.ExecuteScript;
                _handler.StringParam = _lastCodeFromAI;
                _exEvent.Raise();
                return;
            }

            // 2. AI-POWERED COMMAND GENERATION
            AddMessage("AI", "Thinking...");
            
            // Enhanced prompt for Local AI (Ollama)
            // NOTE: Using single quotes inside the prompt to avoid escaping hell
            string systemPrompt = @"You are the BIM'ism AI Agent, an expert Revit API Developer.

" + RevitRequestHandler.LastContext + @"

LIBRARY:
- RevitAI.Select(doc, elements);
- RevitAI.GetWalls(doc); // Returns List<Wall>
- RevitAI.GetFoundations(doc); // Returns List<Element> (Structural Foundations)
- RevitAI.GetColumns(doc); // Returns List<Element> (Structural Columns)
- RevitAI.GetBeams(doc); // Returns List<Element> (Structural Framing/Beams)
- RevitAI.GetLevel(doc, 'Name'); // Returns Level
- RevitAI.GetWallType(doc, 'Name'); // Returns WallType
- RevitAI.CreateWall(doc, start, end, level, type, heightFt);
- RevitAI.CreateWindow(doc, wall, symbol, distAlongWall, offsetZ);
- RevitAI.CreateBox(doc, center, widthFt, depthFt, heightFt);
- RevitAI.MetersToFeet(10); // USE THIS for all metric inputs!
- RevitAI.GetFamilySymbol(doc, 'Window Name'); // To get symbol forCreateWindow

RULES:
1. ONLY write C# logic code. No classes, no namespaces, no 'using'.
2. Revit uses FEET internally. Always convert meters/mm using RevitAI.MetersToFeet().
3. BOX LOGIC:
   - Width/Depth are horizontal floor dimensions.
   - Height is vertical.
   - If user says 'box' and gives 1 length (e.g. 4m long), use it for BOTH width and depth.
   - Example: '4m long box, 10m height' -> CreateBox(doc, center, MetersToFeet(4), MetersToFeet(4), MetersToFeet(10));
4. Define variables before usage.
5. PERSONALITY & LANGUAGE: You are 'BIM'ism AI', a pro Sri Lankan Revit Developer. 
   - Talk naturally in Singlish (English + Sinhala mix).
   - Use friendly terms like: 'machan', 'elakiri', 'wade karannam', 'puluwan', 'moko wenne'.
   - If the user says 'machan' or 'hi', just greet them back naturally without any code.
6. TASK LOGIC:
   - If user asks for a Revit task: Explain briefly in Singlish, then provide the C# code in a ```csharp block.
   - If user is just chatting or asking a question (e.g., 'what can you do?'): Respond in Singlish. DO NOT PROVIDE CODE BLOCKS.
7. CAPABILITIES: If asked what you can do, tell them you can:
   - Create Walls, Windows, and Boxes.
   - Select Foundations, Columns, Beams, and Walls.
   - Get Levels and Wall Types.
   - Convert Metric (m/mm) to Feet automatically.
8. RULES FOR CODE:
   - NO classes, NO using, NO namespaces.
   - Use ONLY the LIBRARY methods provided.
   - If you don't know the answer, say 'Sry machan, eka mata thama ba' (I can't do that yet).

EXAMPLES:
User: 'machan'
AI: 'Ow machan! Moko wenna ona? Revit eke mona hari wadeyak thiyenawada karanna? Elakiri.'

User: 'oyata monada karanna puluwan?'
AI: 'Ona deyak machan! Mata puluwan Walls, Windows, Boxes create karanna, slab/foundation select karanna, unit convert karanna wage godak dewal. Mokadda dhang karanna ona?'

User: 'Select all foundations'
AI: 'Hari machan, mama okkoma foundations tika select karala dhennam. Wade hari!
```csharp
var foundations = RevitAI.GetFoundations(doc);
RevitAI.Select(doc, foundations);
```'

USER REQUEST: " + userText + @"
RESPONSE (Singlish Explanation + Code Block only if needed):";

            string aiResponse = await Task.Run(() => _ai.GetAIResponse(systemPrompt));
            
            // Remove "Thinking..."
            ChatHistoryPanel.Children.RemoveAt(ChatHistoryPanel.Children.Count - 1);

            // Check for timeout or connection errors
            if (aiResponse.Contains("HttpClient.Timeout") || aiResponse.Contains("request was canceled") || 
                aiResponse.Contains("300 seconds elapsing"))
            {
                AddMessage("AI", "⏱️ Timeout: Ollama took too long to respond. This usually means:\n\n" +
                    "1. The model is still loading (first run takes time)\n" +
                    "2. Your request is too complex\n" +
                    "3. Your PC needs more resources\n\n" +
                    "💡 Try: Click the 'try again' button below or use a simpler command.");
                AddRetryButton(userText);
                return;
            }

            // Check for Ollama not running
            if (aiResponse.Contains("Cannot connect to Ollama"))
            {
                AddMessage("AI", "❌ Ollama is not running!\n\n" +
                    "Please start Ollama:\n" +
                    "1. Open Windows Terminal\n" +
                    "2. Run: ollama serve\n" +
                    "3. Try your request again");
                return;
            }

            // Extract code from response
            string code = ExtractCode(aiResponse);
            
            AddMessage("AI", aiResponse);

            if (!string.IsNullOrEmpty(code))
            {
                _lastCodeFromAI = code; // CACHE IT for "do it"
                AddMessage("System", "🛠️ Executing Revit Command...");
                _handler.Request = RequestId.ExecuteScript;
                _handler.StringParam = code;
                _exEvent.Raise();
            }
        }

        private void AddRetryButton(string originalPrompt)
        {
            Button retryBtn = new Button
            {
                Content = "🔄 Try Again",
                Height = 30,
                Width = 100,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            retryBtn.Click += async (s, e) =>
            {
                InputBox.Text = originalPrompt;
                await ProcessInput();
            };

            ChatHistoryPanel.Children.Add(retryBtn);
            ChatScroll.ScrollToEnd();
        }

        private string ExtractCode(string response)
        {
            try
            {
                // 1. Check for JSON format
                if (response.Contains("\"script\""))
                {
                    string json = response;
                    if (json.Contains("```json"))
                        json = json.Split(new[] { "```json" }, StringSplitOptions.None)[1].Split(new[] { "```" }, StringSplitOptions.None)[0];
                    
                    dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    if (obj.script != null)
                        return CleanCode(obj.script.ToString());
                }
            }
            catch { }

            // CRITICAL: Detect Ollama Timeouts/Errors pretending to be code
            if (response.Contains("HttpClient.Timeout") || response.Contains("request was canceled"))
            {
                return null; // This is an error message, not code
            }

            // 2. Check for code blocks
            if (response.Contains("```"))
            {
                int start = response.IndexOf("```");
                int end = response.LastIndexOf("```");
                if (start != end)
                {
                    string block = response.Substring(start + 3, end - start - 3);
                    if (block.StartsWith("csharp")) block = block.Substring(6);
                    else if (block.StartsWith("cs")) block = block.Substring(2);
                    
                    return CleanCode(block);
                }
            }

            // 3. Fallback: Only return if it LOOKS like code (Has semicolons and typical keywords)
            if (IsRevitCode(response))
            {
                 return CleanCode(response);
            }

            return null; // No code found (Just chat)
        }

        private bool IsRevitCode(string code)
        {
            // Simple heuristic to ensure it's actually a Revit script and not just a C# example
            return code.Contains("doc.") || 
                   code.Contains("RevitAI.") || 
                   code.Contains("FilteredElementCollector") || 
                   code.Contains("Wall.") || 
                   code.Contains("Element") ||
                   code.Contains("Transaction") ||
                   code.Contains("XYZ");
        }

        private string CleanCode(string code)
        {
            // ULTRA-AGGRESSIVE STRIPPER V5
            // 1. Remove Markdown Wrapper if present
            code = code.Replace("```csharp", "").Replace("```cs", "").Replace("```", "").Trim();

            // 2. Remove all structural wrappers using RegEx (Multi-line support)
            // Remove Usings
            code = System.Text.RegularExpressions.Regex.Replace(code, @"using\s+[\w\.\s=]+;", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            
            // Remove namespaces, classes, and typical method signatures
            string[] patternsToRemove = new[] {
                @"namespace\s+[\w\.]+\s*\{?",
                @"public\s+class\s+\w+\s*\{?",
                @"internal\s+class\s+\w+\s*\{?",
                @"public\s+void\s+\w+\s*\([^\)]*\)\s*\{?",
                @"public\s+static\s+void\s+\w+\s*\([^\)]*\)\s*\{?",
                @"protected\s+void\s+\w+\s*\([^\)]*\)\s*\{?",
                @"private\s+void\s+\w+\s*\([^\)]*\)\s*\{?",
                @"\[[^\]]+\]", // Remove attributes like [Transaction]
            };

            foreach (var pattern in patternsToRemove)
            {
                code = System.Text.RegularExpressions.Regex.Replace(code, pattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            }

            // 3. Line-by-line cleanup (more surgical)
            string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            foreach (string line in lines)
            {
                string l = line.Trim();
                
                // Skip structural headers that might be split across lines
                if (l.StartsWith("using ") || l.StartsWith("namespace ") || l.StartsWith("public class ") || l.StartsWith("internal class ")) continue;
                if (l.StartsWith("public void") || l.StartsWith("public static void") || l.StartsWith("public override")) continue;
                
                // Skip isolated braces that might be left over from wrappers
                if (l == "{" || l == "}") continue; 

                // Skip common Revit boilerplate if AI includes it
                if (l.Contains("Transaction") && (l.Contains("tx =") || l.Contains("Transaction("))) continue;
                if (l.Contains("tx.Start") || l.Contains("tx.Commit")) continue;
                if (l.Contains("doc.Regenerate")) continue; 
                
                sb.AppendLine(line);
            }

            string cleaned = sb.ToString().Trim();
            
            // Final check: if the AI ONLY output a method signature without body, cleaning might leave it empty.
            if (string.IsNullOrEmpty(cleaned)) return null;

            return FixCommonHallucinations(cleaned);
        }

        private string FixCommonHallucinations(string code)
        {
            // 1. Fix Property Initialization Hallucinations
            if (code.Contains(".X =")) 
                code = System.Text.RegularExpressions.Regex.Replace(code, @"(\w+)\.X\s*=\s*([^;]+);", "$1 = new XYZ($2, $1.Y, $1.Z);");
            if (code.Contains(".Y ="))
                code = System.Text.RegularExpressions.Regex.Replace(code, @"(\w+)\.Y\s*=\s*([^;]+);", "$1 = new XYZ($1.X, $2, $1.Z);");

            // 2. Fix Revit API Method Hallucinations
            if (code.Contains("new Line("))
                code = System.Text.RegularExpressions.Regex.Replace(code, @"new Line\(([^,]+),([^)]+)\)", "Line.CreateBound($1, $2)");

            // 3. Fix: Filters and Collections
            string[] usageTypes = new[] { "Level", "WallType", "FamilySymbol", "Element" };
            foreach (var type in usageTypes)
            {
                string pattern = $@"FilteredElementCollector\(doc\)\.OfClass\(typeof\({type}\)\);";
                if (code.Contains(pattern))
                    code = code.Replace(pattern, $"FilteredElementCollector(doc).OfClass(typeof({type})).FirstElement() as {type};");
            }

            // 4. Fix CS0201 (Invalid Statements)
            if (code.Contains(").Id;"))
                code = System.Text.RegularExpressions.Regex.Replace(code, @"\)\.Id;", ");");

            // 5. Fix: Alias missing methods
            // Note: GetLevels and GetWallTypes were added to RevitAIHelpers, so these might not be needed anymore,
            // but keeping them doesn't hurt.
            
            return code;
        }


        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== BIMism AI Agent Chat History ===");
            sb.AppendLine($"Date: {DateTime.Now}");
            sb.AppendLine("-----------------------------------");

            foreach (var child in ChatHistoryPanel.Children)
            {
                if (child is Border border && border.Child is TextBlock block)
                {
                    string role = border.HorizontalAlignment == HorizontalAlignment.Right ? "You" : "AI";
                    sb.AppendLine($"[{role}]: {block.Text}");
                    sb.AppendLine();
                }
            }

            Clipboard.SetText(sb.ToString());
            TaskDialog.Show("Export Successful", "Chat history has been copied to your clipboard! You can now paste it here for me.");
        }

        private void AddMessage(string sender, string text)
        {
            Border border = new Border
            {
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(10),
                Background = sender == "You" ? new SolidColorBrush(Color.FromRgb(240, 240, 240)) : new SolidColorBrush(Color.FromRgb(220, 240, 255)),
                BorderBrush = sender == "You" ? Brushes.LightGray : new SolidColorBrush(Color.FromRgb(180, 210, 240)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = sender == "You" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 320
            };

            TextBlock block = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            };

            border.Child = block;
            ChatHistoryPanel.Children.Add(border);
            ChatScroll.ScrollToEnd();
        }

        // ==========================================
        // MODULE 2: QA/QC LOGIC
        // ==========================================
        private void RunHealthCheck_Click(object sender, RoutedEventArgs e)
        {
            QAResultsBox.Text = "Running Audit... Please Wait.";
            
            // Hardcoded "Best Practices" script
            string qaScript = @"
                var walls = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().ToElements();
                int dwgCount = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).GetElementCount();
                int unnamedRooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements().Where(r => r.Name == ""Room"").Count();
                
                string report = ""✅ Audit Complete\n"";
                report += $""- Walls Checked: {walls.Count}\n"";
                report += $""- Imported DWGs Found: {dwgCount}"" + (dwgCount > 0 ? "" ⚠️"" : "" ✅"") + ""\n"";
                report += $""- Unnamed Rooms: {unnamedRooms}"" + (unnamedRooms > 0 ? "" ⚠️"" : "" ✅"");
                
                TaskDialog.Show(""QA Report"", report);
            ";

            _handler.Request = RequestId.ExecuteScript;
            _handler.StringParam = qaScript;
            _exEvent.Raise();
            
            AddMessage("System", "Running Model Health Check...");
        }

        // ==========================================
        // MODULE 3: DATA LOGIC
        // ==========================================
        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            // Script to export Walls to CSV (Desktop)
            string csvPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Wall_Schedule.csv");
            string exportScript = $@"
                var walls = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().ToElements();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(""Id,Type,Length,Volume"");
                foreach (Wall w in walls) {{
                    sb.AppendLine($""{{w.Id}},{{w.Name}},{{w.LookupParameter(""Length"").AsValueString()}},{{w.LookupParameter(""Volume"").AsValueString()}}"");
                }}
                System.IO.File.WriteAllText(@""{csvPath}"", sb.ToString());
                TaskDialog.Show(""Export"", ""Wall Schedule saved to Desktop!"");
            ";

            _handler.Request = RequestId.ExecuteScript;
            _handler.StringParam = exportScript;
            _exEvent.Raise();
            
            AddMessage("System", "Exporting Wall Data...");
        }

        private void ExportRooms_Click(object sender, RoutedEventArgs e)
        {
             // Script to export Rooms to CSV (Desktop)
            string csvPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Room_BOQ.csv");
            string exportScript = $@"
                var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(""Id,Name,Number,Area"");
                foreach (Room r in rooms) {{
                    sb.AppendLine($""{{r.Id}},{{r.Name}},{{r.Number}},{{r.Area}}"");
                }}
                System.IO.File.WriteAllText(@""{csvPath}"", sb.ToString());
                TaskDialog.Show(""Export"", ""Room BOQ saved to Desktop!"");
            ";

            _handler.Request = RequestId.ExecuteScript;
            _handler.StringParam = exportScript;
            _exEvent.Raise();
             AddMessage("System", "Exporting Room BOQ...");
        }

        private void BulkSetParam_Click(object sender, RoutedEventArgs e)
        {
            string pName = ParamNameBox.Text;
            string pVal = ParamValueBox.Text;
            
            // Apply to CURRENT SELECTION
            string updateScript = $@"
                var selIds = new UIDocument(doc).Selection.GetElementIds();
                if (selIds.Count == 0) {{ TaskDialog.Show(""Error"", ""Select elements first!""); return; }}
                
                using (Transaction t = new Transaction(doc, ""Bulk Update"")) {{
                    t.Start();
                    foreach (var id in selIds) {{
                        Element el = doc.GetElement(id);
                        Parameter p = el.LookupParameter(""{pName}"");
                        if (p != null && !p.IsReadOnly) p.Set(""{pVal}"");
                    }}
                    t.Commit();
                }}
                TaskDialog.Show(""Success"", ""Updated parameters on selected elements."");
            ";

             _handler.Request = RequestId.ExecuteScript;
            _handler.StringParam = updateScript;
            _exEvent.Raise();
            AddMessage("System", $"Setting {pName} = '{pVal}' on selection...");
        }
    }
}
