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

namespace DLLirant.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        readonly DataContextViewModel data = new DataContextViewModel();
        
        PEAnalyser peAnalyser;

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
            Button button = (Button)sender;
            TextBlock textBlock = button.FindChild<TextBlock>();
            if (!isStarted)
            {
                textBlock.Text = "Stop";
                isStarted = true;
            } else {
                textBlock.Text = "Find DLL Hijackings";
                isStarted = false;
            }

            SynchronizationContext uiContext = SynchronizationContext.Current;
            FileOperations fileOp = new FileOperations();
            CodeGenerator codeGenerator = new CodeGenerator();

            if (isStarted) {
                if (MessageBox.Show("Do you want to test dll hijackings? If you click on NO, the application will go to live debugging to recover potential files that could be hijacked.", "Test dll hijackings?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                DisplayPEFileInformations();
                data.Logs.Add($"Starting the original pefile {Path.GetFileName(peAnalyser.SelectedBinaryPath)} to find live modules hijackings...");
                await Task.Run(() =>
                {
                    List<string> modulesList = new List<string>();
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Normal,
                        FileName = peAnalyser.SelectedBinaryPath
                    };
                    process.StartInfo = startInfo;
                    process.Start();

                    while (!process.HasExited)
                    {
                        try
                        {
                            process.WaitForExit(2000);
                            foreach (ProcessModule module in process.Modules)
                            {
                                if (module.ModuleName != Path.GetFileName(peAnalyser.SelectedBinaryPath))
                                {
                                    if (!module.FileName.ToLower().StartsWith("c:\\windows\\system32") && !modulesList.Contains(module.FileName))
                                    {
                                        PEAnalyser pe = new PEAnalyser(module.FileName);
                                        uiContext.Send(x => data.Logs.Add($"MODULE: {module.FileName} - {pe.CheckIfSigned()}"), null);
                                        modulesList.Add(module.FileName);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    if (modulesList.Count > 0)
                    {
                        fileOp.SaveDllLivePaths("live-paths-found.txt", peAnalyser.SelectedBinaryPath, modulesList);
                    }
                });
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
