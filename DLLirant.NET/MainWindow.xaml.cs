using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using DLLirant.NET.Classes;
using System.Threading.Tasks;
using PeNet;

namespace DLLirant.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        LogsViewModel logs = new LogsViewModel();

        string SelectedBinary;

        List<string> dllExcludes = new List<string>
        {
            "api-ms",
            "ext-ms",
            "ntdll",
            "kernel32",
            "user32",
            "shell32",
            "comctl32",
            "imm32",
            "gdi32",
            "msvcr",
            "ws2_32",
            "ole32",
            "ninput",
            "setupapi",
            "mscoree",
            "msvcp_win",
            "oleaut32",
            "advapi32",
            "crypt32"
        };
        public MainWindow()
        {
            InitializeComponent();
            DataContext = logs;
            logs.Clear();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TextBlock textBlock = button.FindChild<TextBlock>();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PE files (*.exe)|*.exe";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedBinary = openFileDialog.FileName;
                textBlock.Text = Path.GetFileName(SelectedBinary);
                if (SelectedBinary.EndsWith(".exe"))
                {
                    ButtonStart.IsEnabled = true;
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
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
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/redteamsocietegenerale/DLLirant");
        }

        private void DisplayPEFileInformations(PeFile peFile)
        {
            logs.Clear();
            logs.Add($"Selected Binary: {SelectedBinary}");
            if (peFile.IsSigned)
            {
                logs.Add("Is signed: Yes");
            }
            else
            {
                logs.Add("Is signed: No");
            }

            if (peFile.HasValidSignature)
            {
                logs.Add("Is signature valid: Yes");
            }
            else
            {
                logs.Add("Is signature valid: No");
            }

            if (peFile.Is64Bit)
            {
                logs.Add("Architecture: x64");
            }
            else if (peFile.Is32Bit)
            {
                logs.Add("Architecture: x86");
            }
            else
            {
                logs.Add("Architecture: Unknown");
            }

            logs.Add($"MD5: {peFile.Md5}");
            logs.Add($"SHA1: {peFile.Sha1}");
            logs.Add($"SHA256: {peFile.Sha256}");
            logs.Add($"===========================================================");
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            SynchronizationContext uiContext = SynchronizationContext.Current;
            PeFile peFile = new PeFile(SelectedBinary);
            FileOperations fileOp = new FileOperations();
            CodeGenerator codeGenerator = new CodeGenerator();

            DisplayPEFileInformations(peFile);

            List<string> testedModules = new List<string>();
            foreach (PeNet.Header.Pe.ImportFunction func in peFile.ImportedFunctions)
            {
                bool isExcluded = false;
                foreach (string exclude in dllExcludes)
                {
                    if (func.DLL.ToLower().Contains(exclude.ToLower()))
                    {
                        isExcluded = true;
                        break;
                    }
                }

                if (!isExcluded && !testedModules.Contains(func.DLL))
                {
                    fileOp.RecreateOutputDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                    DisplayPEFileInformations(peFile);
                    fileOp.CopyFilesDirToDir(func.DLL, "import/", "output/");

                    logs.Add($"Testing {func.DLL}...");

                    // Testing DllMain
                    logs.Add("DllMain");

                    await Task.Run(() =>
                    {
                        codeGenerator.GenerateDLL("CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);");
                        fileOp.CopyFile(SelectedBinary);
                        fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{func.DLL}");
                    });

                    logs.Add("==========================");
                    await Task.Run(() =>
                    {
                        if (codeGenerator.StartExecutable(Path.GetFileName(SelectedBinary)))
                        {
                            uiContext.Send(x => logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(SelectedBinary)} with the DLL {func.DLL} !"), null);
                            using (StreamWriter sw = File.AppendText("DLLirant-results.txt"))
                            {
                                sw.WriteLine($"[+] DLL SEARCH ORDER HIJACKING FOUND IN: {func.DLL}");
                                sw.WriteLine($"BINARY: {SelectedBinary}");
                                sw.WriteLine($"DllMain\n\n");
                            }
                        }
                    });

                    // Testing others functions.
                    List<string> importedFunctions = GetImportedFunctions(peFile, func.DLL);
                    List<string> functionsToTest = new List<string>();

                    foreach (string importedFunc in importedFunctions)
                    {
                        fileOp.RecreateOutputDirectories(new List<string> { "output/", "C:\\DLLirant\\" });
                        DisplayPEFileInformations(peFile);
                        fileOp.CopyFilesDirToDir(func.DLL, "import/", "output/");

                        logs.Add($"Testing {func.DLL}...");

                        functionsToTest.Add(importedFunc);
                        foreach (string function in functionsToTest)
                        {
                            logs.Add(function);
                        }

                        await Task.Run(() =>
                        {
                            codeGenerator.GenerateDLL(string.Empty, functionsToTest);
                            fileOp.CopyFile(SelectedBinary);
                            fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{func.DLL}");
                        });

                        logs.Add("==========================");
                        await Task.Run(() =>
                        {
                            if (codeGenerator.StartExecutable(Path.GetFileName(SelectedBinary)))
                            {
                                uiContext.Send(x => logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(SelectedBinary)} with the DLL {func.DLL} !"), null);
                                using (StreamWriter sw = File.AppendText("DLLirant-results.txt"))
                                {
                                    sw.WriteLine($"[+] DLL SEARCH ORDER HIJACKING FOUND IN: {func.DLL}\n");
                                    sw.WriteLine($"BINARY: {SelectedBinary}");
                                    foreach (string function in functionsToTest)
                                    {
                                        sw.WriteLine($"extern \"C\" __declspec(dllexport) void {function}() {{ Main(); }}");
                                    }
                                    sw.WriteLine("\n");
                                }
                            }
                        });
                    }

                    testedModules.Add(func.DLL);
                }
            }
            logs.Add("Done.");
        }

        private List<string> GetImportedFunctions(PeFile peFile, string module)
        {
            List<string> importedFunctions = new List<string>();
            foreach (PeNet.Header.Pe.ImportFunction func in peFile.ImportedFunctions)
            {
                if (func.DLL == module)
                    if (func.Name != null)
                        importedFunctions.Add($"extern \"C\" __declspec(dllexport) void {func.Name}() {{ Main(); }}");
            }
            return importedFunctions;
        }
    }
}
