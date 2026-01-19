using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis; // Roslyn
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace RevitAIAgent
{
    public class ScriptRunner
    {
        public static string RunScript(UIApplication uiapp, string code)
        {
            // 1. Wrap the code in a valid class structure
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure; // Fix CS0122 StructuralType
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitAIAgent;

namespace RevitAIAgentDynamic
{
    public class DynamicCommand
    {
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            " + code + @"
        }
    }
}";

            // 2. Parse Syntax Tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            // 3. references
            // Core .NET references
            List<MetadataReference> references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Autodesk.Revit.DB.Document).Assembly.Location), // RevitAPI
                MetadataReference.CreateFromFile(typeof(Autodesk.Revit.UI.TaskDialog).Assembly.Location), // RevitAPIUI
                MetadataReference.CreateFromFile(typeof(ScriptRunner).Assembly.Location) // RevitAIAgent (Self)
            };

            // Add basic system references explicitly for .NET 8
            var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
            var needed = new[] { "System.Runtime", "System.Collections", "System.Console", "netstandard" };
            foreach (var path in trustedAssembliesPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (needed.Any(n => fileName.StartsWith(n)))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }

            // 4. Compile
            CSharpCompilation compilation = CSharpCompilation.Create(
                "DynamicAssembly_" + Guid.NewGuid(),
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    StringBuilder failures = new StringBuilder();
                    foreach (Diagnostic diagnostic in result.Diagnostics)
                    {
                        failures.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }
                    string errorMsg = failures.ToString();
                    // TaskDialog.Show("Compilation Error", errorMsg); // Removed TaskDialog
                    return errorMsg;
                }
                else
                {
                    // 5. Load and Execute
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    
                    Type type = assembly.GetType("RevitAIAgentDynamic.DynamicCommand");
                    object obj = Activator.CreateInstance(type);
                    
                    MethodInfo method = type.GetMethod("Execute");
                    
                    try
                    {
                        method.Invoke(obj, new object[] { uiapp });
                        return null; // Success
                    }
                    catch (TargetInvocationException ex)
                    {
                        // string detailedError = ex.InnerException != null 
                        //     ? ex.InnerException.Message + "\n" + ex.InnerException.StackTrace 
                        //     : ex.Message;
                        // TaskDialog.Show("Runtime Error", "Script failed during execution:\n\n" + detailedError);
                        return "Runtime Error: " + (ex.InnerException?.Message ?? ex.Message);
                    }
                    catch (Exception ex)
                    {
                        return "Unexpected Error: " + ex.Message;
                    }
                }
            }
        }
    }
}
