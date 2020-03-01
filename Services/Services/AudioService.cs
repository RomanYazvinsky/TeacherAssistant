using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using TeacherAssistant.Models;
using TeacherAssistant.Notifications;
using ToastNotifications;

namespace TeacherAssistant.Services {
    public class AudioService {
        private readonly Notifier _notifier;

        public AudioService(Notifier notifier) {
            _notifier = notifier;
        }
        public void AddResource(FileInfo file) {
            var absolutePath = Path.Combine("resources", "sounds", file.Name);
            if (file.FullName.Equals(absolutePath)) {
                return;
            }

            if (!Directory.Exists("resources")) {
                Directory.CreateDirectory("resources");
            }

            if (!Directory.Exists("resources/sounds")) {
                Directory.CreateDirectory("resources/sounds");
            }

            file.CopyTo(absolutePath, true);
        }

        public async Task PlayAlarm(AlarmEntity alarm) {
            WaveStream waveProvider;
            Stream stream;
            if (string.IsNullOrWhiteSpace(alarm.ResourceName)) {
                if (alarm.Sound == null || alarm.Sound.Length == 0) {
                    return;
                }
                stream = new MemoryStream(alarm.Sound);
            }
            else {
                stream = File.OpenRead(Path.Combine("resources", "sounds", alarm.ResourceName));
            }
            switch (alarm.Discriminator) {
                case ".mp3": {
                    waveProvider = new Mp3FileReader(stream);
                    break;
                }
                case ".wav": {
                    waveProvider = new WaveFileReader(stream);
                    break;
                }
                default: {
                    return;
                }
            }

            using (waveProvider)
            using (var wo = new WaveOutEvent()) {
                var observable = Observable.FromEventPattern<StoppedEventArgs>(
                    h => wo.PlaybackStopped += h,
                    h => wo.PlaybackStopped -= h
                );
                wo.Init(waveProvider);
                if (waveProvider.TotalTime > TimeSpan.FromMilliseconds(5000)) {
                    _notifier.ShowAudioNotification(wo);
                }

                wo.Volume = (float)alarm.Volume;
                wo.Play();
                await observable.FirstAsync();
            }
        }
    }
}
