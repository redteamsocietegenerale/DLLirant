using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DLLirant.NET
{
    public class DataContextViewModel : INotifyPropertyChanged
    {
        public DataContextViewModel()
        {
            logs = new ObservableCollection<string>();
            logs.Add("test1");
            logs.Add("test2");
            logs.Add("test3");
            logs.Add("test4");
            logs.Add("test5");
            logs.Add("test6");

            excludesDlls = new ObservableCollection<string>();
            excludesDlls.Add("api-ms");
            excludesDlls.Add("ext-ms");
            excludesDlls.Add("ntdll");
            excludesDlls.Add("kernel32");
            excludesDlls.Add("user32");
            excludesDlls.Add("shell32");
            excludesDlls.Add("comctl32");
            excludesDlls.Add("imm32");
            excludesDlls.Add("gdi32");
            excludesDlls.Add("msvcr");
            excludesDlls.Add("ws2_32");
            excludesDlls.Add("ole32");
            excludesDlls.Add("ninput");
            excludesDlls.Add("setupapi");
            excludesDlls.Add("mscoree");
            excludesDlls.Add("msvcp_win");
            excludesDlls.Add("oleaut32");
            excludesDlls.Add("advapi32");
            excludesDlls.Add("crypt32");
        }

        private ObservableCollection<string> logs;
        public ObservableCollection<string> Logs {
            get { return logs; }
            set
            {
                logs = value;
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
