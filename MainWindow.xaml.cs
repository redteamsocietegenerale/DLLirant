using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using DLLirant.NET.Classes;
using CodeGenerator = DLLirant.NET.Classes.CodeGenerator;

namespace DLLirant.NET
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        SynchronizationContext uiContext = SynchronizationContext.Current;
        CodeGenerator codeGenerator = new CodeGenerator();
        readonly DataContextViewModel data = new DataContextViewModel();

        PEAnalyzer peAnalyzer;

        bool isScanStarted = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = data;
            data.Logs.Clear();
        }

        private void ButtonOpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/redteamsocietegenerale/DLLirant");
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
                    peAnalyzer = new PEAnalyzer(openFileDialog.FileName);
                }
            }
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            Button buttonStart = (Button)sender;
            TextBlock buttonStartTextBlock = buttonStart.FindChild<TextBlock>();

            if (!isScanStarted)
            {
                buttonStartTextBlock.Text = "Stop";
                isScanStarted = true;
            } else
            {
                try {
                    codeGenerator.KillProcessAndChildrens(codeGenerator.process.Id);
                } catch (InvalidOperationException)
                {
                    codeGenerator.process = new Process();
                }
                buttonStartTextBlock.Text = "Find DLL Hijackings";
                isScanStarted = false;
            }

            if (isScanStarted)
            {
                if (MessageBox.Show("Do you want to test dll search order hijackings?", "Test dll search order hijackings?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Get Modules.
                    List<string> modules = peAnalyzer.GetModules(data.ExcludesDLLs.ToList());
                    foreach (string module in modules)
                    {
                        if (!isScanStarted) { break; }

                        // Display PE informations, create directories and import files from the import/ directory to the output/ dir.
                        DisplayPEFileInformations();
                        FileOperations.RecreateDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                        FileOperations.CopyFilesDirToDir("import/", "output/", new List<string> { module });

                        // Testing DllMain().
                        await TestingDllMain(module);

                        // Testing imported functions one by one.
                        await TestingImportedFunctions(module);
                    }
                }

                // Testing NAME_NOT_FOUND live debugging method via ETW events.
                if (MessageBox.Show("Do you want to test to find NAME_NOT_FOUND dll search order hijackings?", "Test live debugging?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    buttonStartTextBlock.Text = "Stop";
                    isScanStarted = true;
                    await TestingNameNotFoundLiveDebugging();
                }

                uiContext.Send(x => data.Logs.Add("Done."), null);
                buttonStartTextBlock.Text = "Find DLL Hijackings";
                isScanStarted = false;
            }
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

        private void DisplayPEFileInformations()
        {
            uiContext.Send(x => data.Logs.Clear(), null);
            uiContext.Send(x => data.Logs.Add($"Process Name: {Path.GetFileName(peAnalyzer.SelectedBinaryPath)}"), null);
            foreach (string info in peAnalyzer.GetPEInformations())
            {
                uiContext.Send(x => data.Logs.Add(info), null);
            }
            uiContext.Send(x => data.Logs.Add("==========================================================="), null);
        }

        private async Task TestingDllMain(string module)
        {
            uiContext.Send(x => data.Logs.Add("Testing DllMain..."), null);

            await Task.Run(() =>
            {
                string cppCode = codeGenerator.GenerateDLL("CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);");
                FileOperations.CopyFileToDir(peAnalyzer.SelectedBinaryPath, "output/");
                FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{module}");
                bool isDllHijackingFound = codeGenerator.StartExecutable(Path.GetFileName(peAnalyzer.SelectedBinaryPath));
                if (isDllHijackingFound)
                {
                    uiContext.Send(x => data.Logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(peAnalyzer.SelectedBinaryPath)} with the DLL {module} !"), null);
                    FileOperations.CreateDirectory("dll-hijacks");
                    string fileName = $"{Path.GetFileName(peAnalyzer.SelectedBinaryPath)}-{module}.cpp";
                    using (StreamWriter sw = File.CreateText($"dll-hijacks\\{fileName}"))
                    {
                        sw.WriteLine($"// MD5 Binary: {peAnalyzer.GetMD5()}");
                        sw.WriteLine($"// SHA1 Binary: {peAnalyzer.GetSHA1()}");
                        sw.WriteLine($"// SHA256 Binary: {peAnalyzer.GetSHA256()}");
                        sw.WriteLine(cppCode);
                    }
                    Thread.Sleep(2000);
                }
            });
        }

        private async Task TestingImportedFunctions(string module)
        {
            List<string> importedFunctions = peAnalyzer.GetImportedFunctions(module);
            List<string> functionsToTest = new List<string>();
            foreach (string importedFunc in importedFunctions)
            {
                if (!isScanStarted) { break; }

                // Display PE informations, create directories and import files from the import/ directory to the output/ dir.
                DisplayPEFileInformations();
                FileOperations.RecreateDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                FileOperations.CopyFilesDirToDir("import/", "output/", new List<string> { module });

                uiContext.Send(x => data.Logs.Add($"Testing {module}..."), null);
                functionsToTest.Add($"extern \"C\" __declspec(dllexport) void {importedFunc}() {{ Main(); }}");
                foreach (string function in functionsToTest)
                {
                    uiContext.Send(x => data.Logs.Add(function), null);
                }
                uiContext.Send(x => data.Logs.Add("=========================="), null);

                await Task.Run(() =>
                {
                    string cppCode = codeGenerator.GenerateDLL(string.Empty, functionsToTest);
                    FileOperations.CopyFileToDir(peAnalyzer.SelectedBinaryPath, "output/");
                    FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{module}");
                    bool isDllHijackingFound = codeGenerator.StartExecutable(Path.GetFileName(peAnalyzer.SelectedBinaryPath));
                    if (isDllHijackingFound)
                    {
                        uiContext.Send(x => data.Logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(peAnalyzer.SelectedBinaryPath)} with the DLL {module} !"), null);
                        FileOperations.CreateDirectory("dll-hijacks");
                        string fileName = $"{Path.GetFileName(peAnalyzer.SelectedBinaryPath)}-{module}.cpp";
                        using (StreamWriter sw = File.CreateText($"dll-hijacks\\{fileName}"))
                        {
                            sw.WriteLine($"// MD5 Binary: {peAnalyzer.GetMD5()}");
                            sw.WriteLine($"// SHA1 Binary: {peAnalyzer.GetSHA1()}");
                            sw.WriteLine($"// SHA256 Binary: {peAnalyzer.GetSHA256()}");
                            sw.WriteLine(cppCode);
                        }
                        Thread.Sleep(2000);
                    }
                });
            }
        }

        private async Task TestingNameNotFoundLiveDebugging()
        {
            DisplayPEFileInformations();
            uiContext.Send(x => data.Logs.Add($"Starting the original pefile to find NAME_NOT_FOUND dll search order hijackings (click on STOP to terminate the test)..."), null);
            
            await Task.Run(() =>
            {
                codeGenerator.StartingTraceEventSession(peAnalyzer, uiContext, data);
            });
        }
    }
}
