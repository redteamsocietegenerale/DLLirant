using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DLLirant.Classes
{
    internal class DLLHijackingsHelper
    {
        public ExecuteCommandHelper ExecuteCommandHelper = new ExecuteCommandHelper();

        private SynchronizationContext uiContext;
        private DataContextViewModel data;
        private PEAnalyzer peAnalyzer;

        public async Task FindSearchOrderHijackings(SynchronizationContext uiContext, DataContextViewModel data, PEAnalyzer peAnalyzer)
        {
            this.uiContext = uiContext;
            this.data = data;
            this.peAnalyzer = peAnalyzer;

            // Get Modules.
            List<string> modules = peAnalyzer.GetModules(data.ExcludesDLLs.ToList());
            foreach (string module in modules)
            {
                if (!ExecuteCommandHelper.IsStarted) break;

                // Display PE informations, create directories and import files from the import/ directory to the output/ dir.
                DisplayPEFileInformations();
                FileOperations.RecreateDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                FileOperations.CopyFilesDirToDir("import/", "output/", new List<string> { module });

                await Task.Run(() =>
                {
                    // Testing DllMain().
                    TestingDllMain(module);

                    // Testing imported functions one by one.
                    TestingImportedFunctions(module);
                });
            }

        }

        private void DisplayPEFileInformations()
        {
            uiContext.Send(x => data.LogsGrid1.Clear(), null);
            uiContext.Send(x => data.LogsGrid1.Add($"Process Name: {Path.GetFileName(peAnalyzer.SelectedBinaryPath)}"), null);
            foreach (string info in peAnalyzer.GetPEInformations())
            {
                uiContext.Send(x => data.LogsGrid1.Add(info), null);
            }
            uiContext.Send(x => data.LogsGrid1.Add("==========================================================="), null);
        }

        private void TestingDllMain(string module)
        {
            uiContext.Send(x => data.LogsGrid1.Add("Testing DllMain..."), null);

            string cppCode = CodeGenerator.GenerateDLL("CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);");
            
            ExecuteCommandHelper.ExecuteCommand("cmd.exe", "/C clang++.exe dllmain.cpp -o DLLirantDLL.dll -shared");

            FileOperations.CopyFileToDir(peAnalyzer.SelectedBinaryPath, "output/");
            FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{module}");

            ExecuteCommandHelper.ExecuteCommand(Path.GetFileName(peAnalyzer.SelectedBinaryPath));

            SaveDllHijackingIfFound(module, cppCode);
        }

        private void TestingImportedFunctions(string module)
        {
            List<string> importedFunctions = peAnalyzer.GetImportedFunctions(module);
            List<string> functionsToTest = new List<string>();
            foreach (string importedFunc in importedFunctions)
            {
                if (!ExecuteCommandHelper.IsStarted) break;

                // Display PE informations, create directories and import files from the import/ directory to the output/ dir.
                DisplayPEFileInformations();
                FileOperations.RecreateDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                FileOperations.CopyFilesDirToDir("import/", "output/", new List<string> { module });

                uiContext.Send(x => data.LogsGrid1.Add($"Testing {module}..."), null);
                functionsToTest.Add($"extern \"C\" __declspec(dllexport) void {importedFunc}() {{ Main(); }}");
                foreach (string function in functionsToTest)
                {
                    uiContext.Send(x => data.LogsGrid1.Add(function), null);
                }
                uiContext.Send(x => data.LogsGrid1.Add("=========================="), null);

                string cppCode = CodeGenerator.GenerateDLL(string.Empty, functionsToTest);

                ExecuteCommandHelper.ExecuteCommand("cmd.exe", "/C clang++.exe dllmain.cpp -o DLLirantDLL.dll -shared");

                FileOperations.CopyFileToDir(peAnalyzer.SelectedBinaryPath, "output/");
                FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{module}");

                ExecuteCommandHelper.ExecuteCommand(Path.GetFileName(peAnalyzer.SelectedBinaryPath));

                SaveDllHijackingIfFound(module, cppCode);
            }
        }

        private void SaveDllHijackingIfFound(string moduleName, string cppCode)
        {
            if (!File.Exists("C:\\DLLirant\\output.txt"))
                return;
            
            uiContext.Send(x => data.LogsGrid1.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(peAnalyzer.SelectedBinaryPath)} with the DLL {moduleName} !"), null);
            FileOperations.CreateDirectory("dll-hijacks");
            string fileName = $"{Path.GetFileName(peAnalyzer.SelectedBinaryPath)}-{moduleName}.cpp";
            using (StreamWriter sw = File.CreateText($"dll-hijacks\\{fileName}"))
            {
                sw.WriteLine($"// MD5 Binary: {peAnalyzer.GetMD5()}");
                sw.WriteLine($"// SHA1 Binary: {peAnalyzer.GetSHA1()}");
                sw.WriteLine($"// SHA256 Binary: {peAnalyzer.GetSHA256()}");
                sw.WriteLine(cppCode);
            }
            Thread.Sleep(2000);
        }
    }
}
