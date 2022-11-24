using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DLLirant.Classes
{
    internal class KernelEventsParser
    {
        public bool IsStarted = false;

        TraceEventSession kernelSession;

        public async void StartNameNotFoundTracing(SynchronizationContext uiContext, DataContextViewModel data, string processName = null)
        {
            IsStarted = true;

            await Task.Run(() =>
            {
                uiContext.Send(x => data.LogsGrid2.Clear(), null);

                if (processName != null)
                    processName = processName.Replace(".exe", string.Empty);

                List<string> namesNotFoundPaths = new List<string>();

                using (kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
                {
                    kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.All);
                    kernelSession.Source.Kernel.FileIOCreate += ((FileIOCreateTraceData obj) =>
                    {
                        if (processName != null && processName.Length > 0)
                        {
                            if (obj.ProcessName == processName)
                            {
                                if (!namesNotFoundPaths.Contains(obj.FileName) && !obj.FileName.ToLower().StartsWith(Environment.SystemDirectory.ToLower()) && obj.FileName.ToLower().EndsWith(".dll"))
                                {
                                    if (!File.Exists(obj.FileName) || !Directory.Exists(Path.GetDirectoryName(obj.FileName)))
                                    {
                                        namesNotFoundPaths.Add(obj.FileName);
                                        uiContext.Send(x => data.LogsGrid2.Add(obj.FileName), null);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!namesNotFoundPaths.Contains(obj.FileName) && !obj.FileName.ToLower().StartsWith(Environment.SystemDirectory.ToLower()) && obj.FileName.ToLower().EndsWith(".dll"))
                            {
                                if (!File.Exists(obj.FileName) || !Directory.Exists(Path.GetDirectoryName(obj.FileName)))
                                {
                                    namesNotFoundPaths.Add(obj.FileName);
                                    uiContext.Send(x => data.LogsGrid2.Add(obj.FileName), null);
                                }
                            }
                        }
                    });

                    kernelSession.Source.Process();
                };
            });
        }

        public void StopNameNotFoundTracing()
        {
            IsStarted = false;
            kernelSession.Stop();
        }
    }
}
