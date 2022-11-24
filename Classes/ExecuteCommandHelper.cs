using System;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace DLLirant.Classes
{
    internal class ExecuteCommandHelper
    {
        public Process process = new Process();

        public bool IsStarted = false;

        public void ExecuteCommand(string path, string arguments = null, int maxRetries = 3)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = path,
                WorkingDirectory = $"{Directory.GetCurrentDirectory()}\\output"
            };

            if (arguments != null)
                startInfo.Arguments = arguments;

            process.StartInfo = startInfo;
            process.Start();
            try
            {
                while (!process.HasExited && IsStarted)
                {
                    process.WaitForExit(3000);
                    maxRetries--;
                    if (maxRetries <= 0)
                    {
                        KillProcessAndChildrens(process.Id);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                process = new Process();
            }
        }

        public void Stop()
        {
            IsStarted = false;
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
    }
}
