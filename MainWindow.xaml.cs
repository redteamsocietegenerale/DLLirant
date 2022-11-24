using DLLirant.Classes;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using PeNet;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DLLirant
{
    public partial class MainWindow : MetroWindow
    {
        readonly SynchronizationContext uiContext = SynchronizationContext.Current;
        readonly DataContextViewModel data = new DataContextViewModel();
        readonly KernelEventsParser kernelEventsParser = new KernelEventsParser();
        readonly DLLHijackingsHelper dllHijackingsHelper = new DLLHijackingsHelper();

        private PEAnalyzer peAnalyzer;

        string SelectedDLL;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = data;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            uiContext.Send(x => data.LogsGrid1.Clear(), null);
            uiContext.Send(x => data.LogsGrid2.Clear(), null);
            GridNameNotFoundMenu.Visibility = Visibility.Collapsed;
            ButtonStartFindingDLLSearchOrderHijackings.IsEnabled = false;
        }

        private void ButtonOpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/redteamsocietegenerale/DLLirant");
        }

        private void TextBoxBinaryNameToTest_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Exe Files (.exe)|*.exe",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                textBox.Text = Path.GetFileName(openFileDialog.FileName);
                if (openFileDialog.FileName.EndsWith(".exe"))
                {
                    ButtonStartFindingDLLSearchOrderHijackings.IsEnabled = true;
                    peAnalyzer = new PEAnalyzer(openFileDialog.FileName);
                }
            }
        }

        private async void ButtonStartFindingDLLSearchOrderHijackings_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TextBlock buttonTextBlock = button.FindChild<TextBlock>();

            if (!dllHijackingsHelper.ExecuteCommandHelper.IsStarted)
            {
                buttonTextBlock.Text = "Stop";
                dllHijackingsHelper.ExecuteCommandHelper.IsStarted = true;
                await dllHijackingsHelper.FindSearchOrderHijackings(uiContext, data, peAnalyzer);
            } else
            {
                dllHijackingsHelper.ExecuteCommandHelper.Stop();
                buttonTextBlock.Text = "Start";
            }
        }

        private void ButtonStartMonitoringNameNotFound_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TextBlock buttonTextBlock = button.FindChild<TextBlock>();

            if (!kernelEventsParser.IsStarted)
            {
                TextBoxProcessNameToMonitor.IsEnabled = false;
                buttonTextBlock.Text = "Stop";
                kernelEventsParser.StartNameNotFoundTracing(uiContext, data, TextBoxProcessNameToMonitor.Text);
            } else
            {
                TextBoxProcessNameToMonitor.IsEnabled = true;
                buttonTextBlock.Text = "Start";
                kernelEventsParser.StopNameNotFoundTracing();
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            switch (tabControl.SelectedIndex)
            {
                case 0:
                    GridDLLSearchOrderHijackingMenu.Visibility = Visibility.Visible;
                    GridNameNotFoundMenu.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    GridNameNotFoundMenu.Visibility = Visibility.Visible;
                    GridDLLSearchOrderHijackingMenu.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    GridNameNotFoundMenu.Visibility = Visibility.Collapsed;
                    GridDLLSearchOrderHijackingMenu.Visibility = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }

        private void MenuItemOpenDirectory_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(Path.GetDirectoryName(ListBoxLogsGrid2.SelectedValue.ToString()));
            } catch (System.ComponentModel.Win32Exception) { MessageBox.Show("The specified directory does not exists"); }
        }

        private void ButtonAddExclude_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxExcludeAdd.Text.Length > 0 && !data.ExcludesDLLs.Contains(TextBoxExcludeAdd.Text.ToLower()))
            {
                string text = TextBoxExcludeAdd.Text.ToLower().Replace(" ", string.Empty);
                data.ExcludesDLLs.Add(text);
            }
        }

        private void MenuItemDeleteExcludeDLL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxExcludes.SelectedValue != null)
            {
                data.ExcludesDLLs.Remove(ListBoxExcludes.SelectedValue.ToString());
            }
        }

        private void SelectedTargetedDLL_Button_Click(object sender, RoutedEventArgs e)
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

        private async void GenerateClassicDLL_Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.Content = "Generating...";
            button.IsEnabled = false;

            PeFile peFile = new PeFile(SelectedDLL);

            FileOperations.RecreateDirectories(new List<string> { "output/" });

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
            await Task.Run(() => {
                CodeGenerator codeGenerator = new CodeGenerator();
                codeGenerator.GenerateDLL("Main();", exportedFunctions);

                ExecuteCommandHelper executeCommandHelper = new ExecuteCommandHelper();
                executeCommandHelper.ExecuteCommand("cmd.exe", "/C clang++.exe dllmain.cpp -o DLLirantDLL.dll -shared");
            });

            if (proxyPath.StartsWith("C:"))
            {
                FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{Path.GetFileName(SelectedDLL)}");
            }
            else
            {
                FileOperations.CopyFileToDir(SelectedDLL, "output/");
                FileOperations.RenameFile($"output/{Path.GetFileName(SelectedDLL)}", $"output/{proxyPath}.dll");
                FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{Path.GetFileName(SelectedDLL)}");
            }

            button.Content = "Success!";
            await Task.Run(() => { Thread.Sleep(2000); });
            button.Content = "Generate";
            button.IsEnabled = true;
        }

        private async void GenerateOrdinalBasedDLL_Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.Content = "Generating...";
            button.IsEnabled = false;

            FileOperations.RecreateDirectories(new List<string> { "output/" });

            List<string> exportedFunctions = new List<string>();
            for (int i = 0; i < sliderValue.Value; i++)
            {
                exportedFunctions.Add($"extern \"C\" __declspec(dllexport) void DLLIrant{i}() {{ Main({i}); }};");
            }

            await Task.Run(() => {
                CodeGenerator codeGenerator = new CodeGenerator();
                codeGenerator.GenerateDLL(string.Empty, exportedFunctions, CodeGenerator.TypeDLLHijacking.OrdinalBased);

                ExecuteCommandHelper executeCommandHelper = new ExecuteCommandHelper();
                executeCommandHelper.ExecuteCommand("cmd.exe", "/C clang++.exe dllmain.cpp -o DLLirantDLL.dll -shared");
            });

            string dllName = TextBoxOrdinalDLLName.Text;
            if (dllName.Length > 0)
            {
                if (!dllName.EndsWith(".dll"))
                {
                    dllName = $"{dllName}.dll";
                }
                FileOperations.RenameFile("output/DLLirantDLL.dll", $"output/{dllName}");
            }
            button.Content = "Success!";
            await Task.Run(() => { Thread.Sleep(2000); });
            button.Content = "Generate";
            button.IsEnabled = true;
        }
    }
}
