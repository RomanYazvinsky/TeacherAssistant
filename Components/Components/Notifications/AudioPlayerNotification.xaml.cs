using System.ComponentModel;
using System.Runtime.CompilerServices;
using Containers;
using Containers.Annotations;
using NAudio.Wave;
using TeacherAssistant.ComponentsImpl;
using ToastNotifications;
using ToastNotifications.Core;

namespace TeacherAssistant {
    public static class AudioNotificationProvider {
        public static void ShowAudioNotification(this Notifier notifier, IWavePlayer audio) {
            notifier.Notify(() => new AudioPlayerNotificationModel(audio));
        }
    }
    public partial class AudioPlayerNotification : NotificationDisplayPart {
        public AudioPlayerNotification(NotificationBase model) {
            InitializeComponent();
            Bind(model);
        }
    }
    
    public class AudioPlayerNotificationModel : NotificationBase, INotifyPropertyChanged {
        private NotificationDisplayPart _displayPart;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AudioPlayerNotificationModel(IWavePlayer audio) : base("", new MessageOptions()) {
            this.DisplayPart = (_displayPart = new AudioPlayerNotification(this));
            audio.PlaybackStopped += (sender, args) => Close();
            this.Title = LocalizationContainer.Localization["Аудиофайл"];
            this.DoStop = new ButtonConfig {
                Command = new CommandHandler(() => {
                    audio.Stop();
                    Close();
                }),
                Text = LocalizationContainer.Localization["Стоп"]
            };
        }

        public string Title { get; set; }
        public ButtonConfig DoStop { get; set; }

        public override NotificationDisplayPart DisplayPart { get; }

    }
}