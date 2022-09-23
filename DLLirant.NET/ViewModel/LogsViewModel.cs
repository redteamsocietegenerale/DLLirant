using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DLLirant.NET
{
    public class LogsViewModel : INotifyPropertyChanged
    {
        public LogsViewModel()
        {
            logs = new ObservableCollection<string>();
            logs.Add("test1");
            logs.Add("test2");
            logs.Add("test3");
            logs.Add("test4");
            logs.Add("test5");
            logs.Add("test6");
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

        public void Add(string text)
        {
            logs.Add(text);
        }

        public void Clear()
        {
            logs.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
