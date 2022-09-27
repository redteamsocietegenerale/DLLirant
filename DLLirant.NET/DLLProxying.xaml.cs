using DLLirant.NET.Classes;
using Microsoft.Win32;
using PeNet;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DLLirant.NET
{
    /// <summary>
    /// Interaction logic for DLLProxying.xaml
    /// </summary>
    public partial class DLLProxying : UserControl
    {
        string SelectedDLL;

        public DLLProxying()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "DLL files (*.dll)|*.dll",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedDLL = openFileDialog.FileName;
                if (SelectedDLL.EndsWith(".dll"))
                {
                    ButtonGenerateClassicDLL.IsEnabled = true;
                    TargetedDLL.Text = SelectedDLL;
                }
            }
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.Content = "Generating...";
            button.IsEnabled = false;

            PeFile peFile = new PeFile(SelectedDLL);
            FileOperations fileOp = new FileOperations();
            CodeGenerator codeGenerator = new CodeGenerator();

            fileOp.RecreateOutputDirectories(new List<string> { "output/" });

            List<string> exportedFunctions = new List<string>();
            string proxyPath = "proxy";
            if (TextBoxPathProxyDLL.Text.Length > 0)
            {
                proxyPath = TextBoxPathProxyDLL.Text.Replace(".dll", string.Empty);
            }
            foreach (PeNet.Header.Pe.ExportFunction func in peFile.ExportedFunctions)
            {
                exportedFunctions.Add($"#pragma comment(linker,\"/export:{func.Name}={proxyPath}.{func.Name},@{func.Ordinal}\")");
            }
            await Task.Run(() =>
            {
                codeGenerator.GenerateDLL("Main();", exportedFunctions);
            });

            if (proxyPath.StartsWith("C:"))
            {
                fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{Path.GetFileName(SelectedDLL)}");
            }
            else
            {
                fileOp.CopyFile(SelectedDLL);
                fileOp.RenameFile($"output/{Path.GetFileName(SelectedDLL)}", $"output/{proxyPath}.dll");
                fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{Path.GetFileName(SelectedDLL)}");
            }

            button.Content = "Success!";
            await Task.Run(() =>
            {
                Thread.Sleep(2000);
            });
            button.Content = "Generate";
            button.IsEnabled = true;
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.Content = "Generating...";
            button.IsEnabled = false;
            FileOperations fileOp = new FileOperations();
            CodeGenerator codeGenerator = new CodeGenerator();

            fileOp.RecreateOutputDirectories(new List<string> { "output/" });

            string dllName = TextBoxOrdinalDLLName.Text;
            List<string> exportedFunctions = new List<string>();
            for (int i = 0; i < sliderValue.Value; i++)
            {
                exportedFunctions.Add($"extern \"C\" __declspec(dllexport) void DLLIrant{i}() {{ Main({i}); }};");
            }

            await Task.Run(() =>
            {
                codeGenerator.GenerateDLL(string.Empty, exportedFunctions, CodeGenerator.TypeDLLHijacking.OrdinalBased);
            });
            if (dllName.Length > 0)
            {
                if (!dllName.EndsWith(".dll"))
                {
                    dllName = $"{dllName}.dll";
                }
                fileOp.RenameFile("output/DLLirantDLL.dll", $"output/{dllName}");
            }
            button.Content = "Success!";
            await Task.Run(() =>
            {
                Thread.Sleep(2000);
            });
            button.Content = "Generate";
            button.IsEnabled = true;
        }
    }
}
