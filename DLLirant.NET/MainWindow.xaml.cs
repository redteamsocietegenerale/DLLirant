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
            openFileDialog.Filter = "PE files (*.exe; *.dll)|*.exe; *.dll";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedBinary = openFileDialog.FileName;
                textBlock.Text = Path.GetFileName(SelectedBinary);

                if(SelectedBinary.EndsWith(".dll"))
                {
                    TextBlock textBlockStartButton = ButtonStart.FindChild<TextBlock>();
                    textBlockStartButton.Text = "Proxying DLL";
                    TextBoxProxyDLLName.Text = "Enter your proxy DLL name or a full path to use an existing DLL...";
                    TextBoxProxyDLLName.Visibility = Visibility.Visible;
                } else
                {
                    TextBlock textBlockStartButton = ButtonStart.FindChild<TextBlock>();
                    textBlockStartButton.Text = "Find DLL Hijackings";
                    TextBoxProxyDLLName.Visibility = Visibility.Collapsed;
                }
                ButtonStart.IsEnabled = true;
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Sh0ckFR/DLLirant");
        }

        private void TextBoxProxyDLLName_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.Text = string.Empty;
        }

        private void DisplayPEFileInformations(PeFile peFile)
        {
            logs.Clear();
            logs.Add($"Selected Binary: {SelectedBinary}");
            if (peFile.IsSigned) {
                logs.Add("Is signed: Yes");
            } else {
                logs.Add("Is signed: No");
            }

            if (peFile.HasValidSignature) {
                logs.Add("Is signature valid: Yes");
            } else {
                logs.Add("Is signature valid: No");
            }

            if (peFile.Is64Bit) {
                logs.Add("Architecture: x64");
            } else if (peFile.Is32Bit) {
                logs.Add("Architecture: x86");
            } else {
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

            if (SelectedBinary.EndsWith(".dll")) {
                // Proxying the dll.
                List<string> exportedFunctions = new List<string>();
                string dllName = TextBoxProxyDLLName.Text;
                dllName = dllName.Replace(".dll", string.Empty);
                foreach (PeNet.Header.Pe.ExportFunction func in peFile.ExportedFunctions)
                {
                    exportedFunctions.Add($"#pragma comment(linker,\"/export:{func.Name}={TextBoxProxyDLLName.Text}.{func.Name},@{func.Ordinal}\")");
                }
                await Task.Run(() =>
                {
                    codeGenerator.GenerateDLL("Main();", exportedFunctions);
                });
                // If the dll name startstwith C:, it's an existing dll used, so we just rename the compiled dll as the original dll.
                if(dllName.StartsWith("C:"))
                {
                    fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{Path.GetFileName(SelectedBinary)}");
                    logs.Add($"[+] {Path.GetFileName(SelectedBinary)} proxying generated in output directory, replace the original DLL by this file, it should work");
                } else
                {
                    // Else we copy the original DLL, rename it with the name selected and copy the original dll.
                    fileOp.CopyFile(SelectedBinary);
                    fileOp.RenameFile($"output/{Path.GetFileName(SelectedBinary)}", $"output/{dllName}.dll");
                    logs.Add($"[+] {dllName}.dll (original DLL) and {Path.GetFileName(SelectedBinary)} (generated DLL with the original DLL name) proxying generated in output directory, copy the both files, it should work");
                }
            } else {
                List<string> testedModules = new List<string>();
                foreach (PeNet.Header.Pe.ImportFunction func in peFile.ImportedFunctions)
                {
                    bool isExcluded = false;
                    foreach (string exclude in dllExcludes)
                    {
                        if (func.DLL.ToLower().Contains(exclude))
                        {
                            isExcluded = true;
                            break;
                        }
                    }

                    if (!isExcluded && !testedModules.Contains(func.DLL))
                    {
                        RecreateOutputDirectories();
                        DisplayPEFileInformations(peFile);
                        fileOp.CopyFilesDirToDir(func.DLL, "import/", "output/");

                        logs.Add($"Testing {func.DLL}...");

                        // Testing DllMain
                        logs.Add("DllMain");

                        await Task.Run(() =>
                        {
                            codeGenerator.GenerateDLL("CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);");
                        });

                        fileOp.CopyFile(SelectedBinary);
                        fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{func.DLL}");

                        logs.Add("==========================");
                        if (codeGenerator.StartExecutable(Path.GetFileName(SelectedBinary)))
                        {
                            logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(SelectedBinary)} with the DLL {func.DLL} !");
                            using (StreamWriter sw = File.AppendText("DLLirant-results.txt"))
                            {
                                sw.WriteLine($"[+] DLL SEARCH ORDER HIJACKING FOUND IN: {func.DLL}");
                                sw.WriteLine($"BINARY: {SelectedBinary}");
                                sw.WriteLine($"DllMain\n\n");
                            }
                        }

                        // Testing others functions.
                        List<string> importedFunctions = GetImportedFunctions(peFile, func.DLL);
                        List<string> functionsToTest = new List<string>();

                        foreach (string importedFunc in importedFunctions)
                        {
                            RecreateOutputDirectories();
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
                            });

                            fileOp.CopyFile(SelectedBinary);
                            fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{func.DLL}");

                            logs.Add("==========================");
                            if (codeGenerator.StartExecutable(Path.GetFileName(SelectedBinary)))
                            {
                                logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(SelectedBinary)} with the DLL {func.DLL} !");
                                using (StreamWriter sw = File.AppendText("DLLirant-results.txt"))
                                {
                                    sw.WriteLine($"[+] DLL SEARCH ORDER HIJACKING FOUND IN: {func.DLL}\n");
                                    sw.WriteLine($"BINARY: {SelectedBinary}");
                                    foreach(string function in functionsToTest)
                                    {
                                        sw.WriteLine($"extern \"C\" __declspec(dllexport) void {function}() {{ Main(); }}");
                                    }
                                    sw.WriteLine("\n");
                                }
                            }
                        }

                        testedModules.Add(func.DLL);
                    }
                }
                logs.Add("Done.");
            }
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

        private void RecreateOutputDirectories()
        {
            FileOperations fileOp = new FileOperations();
            fileOp.DeleteDirectory("output/");
            fileOp.CreateDirectory("output/");
            fileOp.DeleteDirectory("C:\\DLLirant\\");
            fileOp.CreateDirectory("C:\\DLLirant\\");
        }
    }
}
