using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using DLLirant.NET.Classes;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace DLLirant.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        readonly DataContextViewModel data = new DataContextViewModel();
        
        PEAnalyser peAnalyser;
        Process process = new Process();
        List<string> modulesList = new List<string>();

        bool isStarted = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = data;
            data.Logs.Clear();
        }

        private void DisplayPEFileInformations()
        {
            data.Logs.Clear();
            foreach (string info in peAnalyser.GetPEInformations())
            {
                data.Logs.Add(info);
            }
            data.Logs.Add("===========================================================");
        }

        private void ButtonSelectBinary_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TextBlock textBlock = button.FindChild<TextBlock>();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PE files (*.exe)|*.exe";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                textBlock.Text = Path.GetFileName(openFileDialog.FileName);
                if (openFileDialog.FileName.EndsWith(".exe"))
                {
                    ButtonStart.IsEnabled = true;
                    peAnalyser = new PEAnalyser(openFileDialog.FileName);
                }
            }
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            SynchronizationContext uiContext = SynchronizationContext.Current;
            FileOperations fileOp = new FileOperations();
            CodeGenerator codeGenerator = new CodeGenerator();

            Button button = (Button)sender;
            TextBlock textBlock = button.FindChild<TextBlock>();
            if (!isStarted)
            {
                textBlock.Text = "Stop";
                isStarted = true;
            } else {
                process.Kill();
                if (modulesList.Count > 0)
                {
                    fileOp.SaveDllLivePaths("name-not-found.txt", peAnalyser.SelectedBinaryPath, modulesList);
                }
                modulesList.Clear();
                textBlock.Text = "Find DLL Hijackings";
                isStarted = false;
            }

            if (isStarted) {
                if (MessageBox.Show("Do you want to test dll search order hijackings?", "Test dll search order hijackings?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Get Modules.
                    List<string> modules = peAnalyser.GetModules(data.ExcludesDLLs.ToList());
                    foreach (string module in modules)
                    {
                        if (isStarted)
                        {
                            bool isDllHijackingFound = false;

                            // Get PE Informations and display them.
                            DisplayPEFileInformations();
                            fileOp.RecreateOutputDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                            fileOp.CopyFilesDirToDir(module, "import/", "output/");

                            // Testing DllMain().
                            data.Logs.Add($"Testing DllMain...");
                            await Task.Run(() =>
                            {
                                codeGenerator.GenerateDLL("CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);");
                                fileOp.CopyFile(peAnalyser.SelectedBinaryPath);
                                fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{module}");
                                isDllHijackingFound = codeGenerator.StartExecutable(Path.GetFileName(peAnalyser.SelectedBinaryPath));
                            });

                            if (isDllHijackingFound)
                            {
                                data.Logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(peAnalyser.SelectedBinaryPath)} with the DLL {module} !");
                                fileOp.SaveDllHijackingLogs("hijackings-found.txt", module, peAnalyser.SelectedBinaryPath, new List<string> { "DllMain" });
                                isDllHijackingFound = false;
                            }

                            // Get Imported Functions and test them one by one.
                            List<string> importedFunctions = peAnalyser.GetImportedFunctions(module);
                            List<string> functionsToTest = new List<string>();
                            foreach (string importedFunc in importedFunctions)
                            {
                                if (isStarted)
                                {
                                    DisplayPEFileInformations();
                                    fileOp.RecreateOutputDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                                    fileOp.CopyFilesDirToDir(module, "import/", "output/");

                                    data.Logs.Add($"Testing {module}...");

                                    functionsToTest.Add($"extern \"C\" __declspec(dllexport) void {importedFunc}() {{ Main(); }}");
                                    foreach (string function in functionsToTest)
                                    {
                                        data.Logs.Add(function);
                                    }
                                    data.Logs.Add("==========================");

                                    await Task.Run(() =>
                                    {
                                        codeGenerator.GenerateDLL(string.Empty, functionsToTest);
                                        fileOp.CopyFile(peAnalyser.SelectedBinaryPath);
                                        fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{module}");
                                        isDllHijackingFound = codeGenerator.StartExecutable(Path.GetFileName(peAnalyser.SelectedBinaryPath));
                                    });

                                    if (isDllHijackingFound)
                                    {
                                        data.Logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(peAnalyser.SelectedBinaryPath)} with the DLL {module} !");
                                        fileOp.SaveDllHijackingLogs("hijackings-found.txt", module, peAnalyser.SelectedBinaryPath, functionsToTest);
                                        fileOp.SaveCppCode($"{Path.GetFileName(peAnalyser.SelectedBinaryPath)}-{module}.cpp", codeGenerator.CppCode);
                                        isDllHijackingFound = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Testing the binary in live debugging to find DLL hijackings via absolute path
            if (isStarted)
            {
                if (MessageBox.Show("Do you want to test to find NAME_NOT_FOUND dll search order hijackings?", "Test live debugging?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DisplayPEFileInformations();
                    data.Logs.Add($"Starting the original pefile {Path.GetFileName(peAnalyser.SelectedBinaryPath)} to find NAME_NOT_FOUND dll search order hijackings (click on STOP to terminate the test)...");
                    await Task.Run(() =>
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Normal,
                            FileName = peAnalyser.SelectedBinaryPath
                        };
                        process.StartInfo = startInfo;
                        process.Start();

                        TraceEventSession kernelSession = null;
                        while (!process.HasExited)
                        {
                            try
                            {
                                process.WaitForExit(100);

                                kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
                                kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.All);
                                kernelSession.Source.Kernel.FileIOCreate += ((FileIOCreateTraceData obj) =>
                                {
                                    if (!data.Logs.Contains($"NAME_NOT_FOUND: {obj.FileName}") && !obj.FileName.ToLower().StartsWith("c:\\windows\\system32\\") && obj.FileName.ToLower().EndsWith(".dll"))
                                    {
                                        if (!File.Exists(obj.FileName))
                                        {
                                            uiContext.Send(x => data.Logs.Add($"NAME_NOT_FOUND: {obj.FileName}"), null);
                                            modulesList.Add(obj.FileName);
                                        }
                                    }
                                });

                                kernelSession.Source.Process();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }

                        if (kernelSession != null)
                        {
                            kernelSession.Stop();
                        }
                    });
                }
            }

            data.Logs.Add("Done.");
            isStarted = false;
            textBlock.Text = "Find DLL Hijackings";
        }

        private void ButtonDllProxying_Click(object sender, RoutedEventArgs e)
        {
            MetroWindow container = new MetroWindow();
            DLLProxying contentControl = new DLLProxying();
            container.Title = "DLL PROXYING";
            container.Width = 700;
            container.Height = 450;
            container.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            container.Content = contentControl;
            container.Show();
        }

        private void ButtonOpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/redteamsocietegenerale/DLLirant");
        }

        private void ButtonAddExclude_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxExcludeAdd.Text.Length > 0 && !data.ExcludesDLLs.Contains(TextBoxExcludeAdd.Text.ToLower()))
            {
                string text = TextBoxExcludeAdd.Text.ToLower().Replace(" ", string.Empty);
                data.ExcludesDLLs.Add(text);
            }
        }

        private void ButtonDeleteExclude_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxExcludes.SelectedValue != null)
            {
                data.ExcludesDLLs.Remove(ListBoxExcludes.SelectedValue.ToString());
            }
        }
    }
}
