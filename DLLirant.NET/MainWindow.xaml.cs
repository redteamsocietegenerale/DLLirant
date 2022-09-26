using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using DLLirant.NET.Classes;
using System.Threading.Tasks;
using System.Diagnostics;
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
            FileOperations fileOp = new FileOperations();
            CodeGenerator codeGenerator = new CodeGenerator();

            // Get PE Informations and display them.
            DisplayPEFileInformations();

            // Get Modules.
            List<string> modules = peAnalyser.GetModules(data.ExcludesDLLs.ToList());
            foreach(string module in modules)
            {
                // Get Imported Functions and test them one by one.
                List<string> importedFunctions = peAnalyser.GetImportedFunctions(module);
                List<string> functionsToTest = new List<string>();
                foreach (string importedFunc in importedFunctions)
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

                    bool isDllHijackingFound = false;
                    await Task.Run(() =>
                    {
                        codeGenerator.GenerateDLL(string.Empty, functionsToTest);
                        fileOp.CopyFile(peAnalyser.SelectedBinaryPath);
                        fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{module}");
                        isDllHijackingFound = codeGenerator.StartExecutable(Path.GetFileName(peAnalyser.SelectedBinaryPath));
                    });

                    if(isDllHijackingFound)
                    {
                        data.Logs.Add($"[+] DLL Search Order Hijacking found in the binary {Path.GetFileName(peAnalyser.SelectedBinaryPath)} with the DLL {module} !");
                        fileOp.SaveDllHijackingLogs("DLLirant-results.txt", module, peAnalyser.SelectedBinaryPath, functionsToTest);
                    }
                }
            }
            data.Logs.Add("Done.");
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
                data.ExcludesDLLs.Add(TextBoxExcludeAdd.Text.ToLower());
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
