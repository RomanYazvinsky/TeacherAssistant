using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Containers.Annotations;
using TeacherAssistant.ComponentsImpl;
using ToastNotifications;
using ToastNotifications.Core;

namespace TeacherAssistant {
    public static class TimerNotificationProvider {
        public static void ShowTimerNotification(this Notifier notifier, DateTime date) {
            notifier.Notify(() => new TimerNotificationModel(date));
        }
    }

    public partial class DynamicNotification : NotificationDisplayPart {
        public DynamicNotification(NotificationBase model) {
            InitializeComponent();
            Bind(model);
            MouseDown += (sender, args) => {
                this.Notification.Close();
            };
        }
    }

    public class TimerNotificationModel : NotificationBase, INotifyPropertyChanged, IDisposable {
        private string _text;
        private IDisposable _timer;
        private NotificationDisplayPart _displayPart;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TimerNotificationModel(DateTime time) : base("", new MessageOptions()) {
            _displayPart = new DynamicNotification(this);
            this.Title = LocalizationContainer.Localization["До следующего звонка:"];
            this.Text = (time - DateTime.Now).ToString(@"hh\:mm\:ss");
            var timer = Observable
                .Interval(TimeSpan.FromMilliseconds(1000))
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(l =>  this.Text = (time - DateTime.Now).ToString(@"hh\:mm\:ss"));
            _timer = timer;
        }

        public string Title { get; set; }

        public string Text {
            get => _text;
            set {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged();
            }
        }

        public override NotificationDisplayPart DisplayPart => _displayPart;

        public void Dispose() {
            _timer.Dispose();
        }
    }
}
