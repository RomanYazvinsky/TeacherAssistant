using System;
using System.ComponentModel;
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
        private DispatcherTimer _timer;
        private NotificationDisplayPart _displayPart;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TimerNotificationModel(DateTime time) : base("", new MessageOptions()) {
            this.DisplayPart = (_displayPart = new DynamicNotification(this));
            Title = AbstractModel.Localization["До следующего звонка:"];
            this.Text = (time - DateTime.Now).ToString(@"hh\:mm\:ss");
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += (sender, args) => { this.Text = (time - DateTime.Now).ToString(@"hh\:mm\:ss"); };
            timer.Start();
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

        public override NotificationDisplayPart DisplayPart { get; }

        public void Dispose() {
            _timer.Stop();
        }
    }
}