using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Threading;

namespace DLLirant.NET.Classes
{
    internal class CodeGenerator
    {
        public Process process = new Process();

        public enum TypeDLLHijacking
        {
            DLLSearchOrderHijacking,
            OrdinalBased
        }

        public string GenerateDLL(string dllmain, List<string> functions = null, TypeDLLHijacking typeDLLHijacking = TypeDLLHijacking.DLLSearchOrderHijacking)
        {
            string CppCode;
            if (typeDLLHijacking == TypeDLLHijacking.DLLSearchOrderHijacking)
            {
                CppCode =
                "#include <windows.h>\r\n" +
                "#include <stdio.h>\r\n\r\n" +

                "#pragma comment (lib, \"User32.lib\")\r\n\r\n" +
                "int Main() {\r\n" +
                    "\tFILE* fptr;\r\n" +
                    "\tfopen_s(&fptr, \"C:\\\\DLLirant\\\\output.txt\", \"w\");\r\n" +
                    "\tfprintf(fptr, \"%s\", \"It works !\");\r\n" +
                    "\tfclose(fptr);\r\n" +
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
                    "\t}\r\n" +
                    "\treturn TRUE;\r\n" +
                    "}\r\n\r\n";
            }
            else
            {
                CppCode =
                "#include <windows.h>\r\n" +
                "#include <string>\r\n" +

                "#pragma comment (lib, \"User32.lib\")\r\n\r\n" +
                "int Main(int nb) {\r\n" +
                    "\tstd::wstring message = std::to_wstring(nb);\r\n" +
                    "\tMessageBoxW(0, message.data(), L\"DLL Hijack\", 0);\r\n" +
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
                    "\t}\r\n" +
                    "\treturn TRUE;\r\n" +
                    "}\r\n\r\n";
            }

            if (functions != null) { CppCode += string.Join("\n", functions.ToArray()); };

            using (StreamWriter writer = new StreamWriter("output/dllmain.cpp"))
            {
                writer.WriteLine(CppCode);
            }

            ExecuteCommand("cmd.exe", "/C clang++.exe dllmain.cpp -o DLLirantDLL.dll -shared");

            return CppCode;
        }

        public bool StartExecutable(string path)
        {
            ExecuteCommand(path);

            if (File.Exists("C:\\DLLirant\\output.txt"))
                return true;
            return false;
        }

        private void ExecuteCommand(string path, string arguments = null, int maxRetries = 3)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = path;
            if (arguments != null)
                startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = $"{Directory.GetCurrentDirectory()}\\output";
            process.StartInfo = startInfo;
            process.Start();
            try
            {
                while (!process.HasExited)
                {
                    process.WaitForExit(3000);
                    maxRetries--;
                    if (maxRetries <= 0)
                    {
                        KillProcessAndChildrens(process.Id);
                    }
                }
            } catch (InvalidOperationException)
            {
                process = new Process();
            }
        }

        public void KillProcessAndChildrens(int pid)
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
            catch (System.ComponentModel.Win32Exception)
            {
                // Access Denied.
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            catch (InvalidOperationException)
            {
                process = new Process();
            }
        }

        public void StartingTraceEventSession(PEAnalyzer peAnalyzer, SynchronizationContext uiContext, DataContextViewModel data)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = peAnalyzer.SelectedBinaryPath
            };

            process.StartInfo = startInfo;
            process.Start();

            try
            {
                while (!process.HasExited)
                {
                    try
                    {
                        process.WaitForExit(100);

                        using (TraceEventSession kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
                        {
                            kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.All);
                            kernelSession.Source.Kernel.FileIOCreate += ((FileIOCreateTraceData obj) =>
                            {
                                if (obj.ProcessName == process.ProcessName)
                                {
                                    if (!data.Logs.Contains($"NAME_NOT_FOUND: {obj.FileName}") && !obj.FileName.ToLower().StartsWith(Environment.SystemDirectory.ToLower()) && obj.FileName.ToLower().EndsWith(".dll"))
                                    {
                                        if (!File.Exists(obj.FileName))
                                        {
                                            uiContext.Send(x => data.Logs.Add($"NAME_NOT_FOUND: {obj.FileName}"), null);
                                        }
                                    }
                                }
                            });

                            kernelSession.Source.Process();
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            } catch (InvalidOperationException)
            {
                process = new Process();
            }
        }
    }
}
