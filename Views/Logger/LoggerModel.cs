using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TeacherAssistant.Components.Annotations;
using TeacherAssistant.State;

namespace TeacherAssistant.Components.Logger
{
    public class LoggerModel : INotifyPropertyChanged, IDisposable
    {
        private string _log;
        public event PropertyChangedEventHandler PropertyChanged;
        private IDisposable _logSubscription;
        public string Log
        {
            get => _log;
            set
            {
                _log = value;
                OnPropertyChanged(nameof(Log));
            }
        }

        public LoggerModel(string id)
        {
            _logSubscription = new Subscriber<string>("Log", log => Log = log).SubscribeOnChanges();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _logSubscription.Dispose();
        }
    }
}