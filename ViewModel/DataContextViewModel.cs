using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DLLirant
{
    internal class DataContextViewModel : INotifyPropertyChanged
    {
        public DataContextViewModel()
        {
            logsgrid1 = new ObservableCollection<string>
            {
                "grid1 test1",
                "grid1 test2",
                "grid1 test3",
                "grid1 test4",
                "grid1 test5",
                "grid1 test6"
            };

            logsgrid2 = new ObservableCollection<string>
            {
                "grid2 test1",
                "grid2 test2",
                "grid2 test3",
                "grid2 test4",
                "grid2 test5",
                "grid2 test6"
            };

            excludesDlls = new ObservableCollection<string>
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
        }

        private ObservableCollection<string> logsgrid1;
        public ObservableCollection<string> LogsGrid1
        {
            get { return logsgrid1; }
            set
            {
                logsgrid1 = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> logsgrid2;
        public ObservableCollection<string> LogsGrid2
        {
            get { return logsgrid2; }
            set
            {
                logsgrid2 = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> excludesDlls;
        public ObservableCollection<string> ExcludesDLLs
        {
            get { return excludesDlls; }
            set
            {
                excludesDlls = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
