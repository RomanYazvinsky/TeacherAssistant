using System;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Model;
using NAudio.Wave;
using TeacherAssistant.State;
using ToastNotifications;

namespace TeacherAssistant {
    public class SoundUtil {
        private static readonly string CurrentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void AddResource(FileInfo file) {
            var absolutePath = Path.Combine(CurrentDir, "resources", "sounds", file.Name);
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

        public static async Task PlayAlarm(AlarmEntity alarm) {
            WaveStream waveProvider;
            var stream = string.IsNullOrWhiteSpace(alarm.ResourceName)
                ? new MemoryStream(alarm._Sound) as Stream
                : File.OpenRead(Path.Combine(CurrentDir, "resources", "sounds", alarm.ResourceName));
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
                var observable = Observable.FromEventPattern<StoppedEventArgs>(handler => wo.PlaybackStopped += handler,
                    handler => wo.PlaybackStopped -= handler);
                wo.Init(waveProvider);
                if (waveProvider.TotalTime > TimeSpan.FromMilliseconds(5000)) {
                    var notifier = Injector.Get<Notifier>();
                    notifier.ShowAudioNotification(wo);
                }

                wo.Volume = (float) alarm.Volume;
                wo.Play();
                await observable.FirstAsync();
            }
        }
    }
}