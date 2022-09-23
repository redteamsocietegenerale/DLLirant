using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace DLLirant.NET.Classes
{
    internal class CodeGenerator
    {
        public void GenerateDLL(string dllmain, List<string> importedFunctions = null)
        {
            string code =
                "#include <windows.h>\r\n" +
                "#include <stdio.h>\r\n\r\n" +

                "#pragma comment (lib, \"User32.lib\")\r\n\r\n" +
                "int Main() {\r\n" +
                    "\tFILE* fptr;\r\n" +
                    "\tfopen_s(&fptr, \"C:\\\\DLLirant\\\\output.txt\", \"w\");\r\n" +
                    "\tfprintf(fptr, \"%s\", \"It works !\");\r\n" +
                    "\tfclose(fptr);\r\n" +
                    "\tMessageBoxW(0, L\"DLL Hijack found!\", L\"DLL Hijack\", 0);\r\n" +
                    "\treturn 1;\r\n" +
                "}\r\n\r\n" +
                "BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)\r\n" +
                "{\r\n" +
                    "\tswitch (ul_reason_for_call) {\r\n" +
                        "\t\tcase DLL_PROCESS_ATTACH:\r\n" +
                            "\t\t\t" + dllmain + "\r\n" +
                            "\t\t\tbreak;\r\n" +
                        "\t\tcase DLL_THREAD_ATTACH:\r\n" +
                        "\t\tcase DLL_THREAD_DETACH:\r\n" +
                        "\t\tcase DLL_PROCESS_DETACH:\r\n" +
                            "\t\t\tbreak;\r\n" +
                    "\t}\r\n"+
                    "\treturn TRUE;\r\n"+
                    "}\r\n\r\n";
                
            if (importedFunctions != null) { code += string.Join("\n", importedFunctions.ToArray()); };

            using (StreamWriter writer = new StreamWriter("output/dllmain.cpp"))
            {
                writer.WriteLine(code);
            }

            ExecuteCommand("cmd.exe", "/C clang++.exe dllmain.cpp -o DLLirantDLL.dll -shared");
        }

        public bool StartExecutable(string path)
        {
            ExecuteCommand(path);

            if (File.Exists("C:\\DLLirant\\output.txt"))
                return true;
            return false;
        }

        private void ExecuteCommand(string path, string arguments = null)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = path;
            if (arguments != null)
                startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = $"{Directory.GetCurrentDirectory()}\\output";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit(10000);
            KillProcessAndChildrens(process.Id);
        }

        private static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"]));
                }

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}
