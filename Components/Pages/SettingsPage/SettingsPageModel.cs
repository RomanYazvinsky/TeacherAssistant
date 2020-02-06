using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using Containers;
using DynamicData;
using Microsoft.Win32;
using Model;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Pages.SettingsPage {
    public class SettingsPageModel : AbstractModel<SettingsPageModel> {
        public SettingsPageModel() {
            this.RefreshSubject.AsObservable().Subscribe(_ => {
                this.Alarms.Clear();
                this.Alarms.AddRange(LocalDbContext.Instance.Alarms
                    .Select(alarm => new AlarmSettingsViewModel(alarm)).ToList());
            });
        }

        public ObservableCollection<AlarmSettingsViewModel> Alarms { get; set; } =
            new ObservableCollection<AlarmSettingsViewModel>();

        protected override string GetLocalizationKey() {
            return "settings";
        }
    }

    public class AlarmSettingsViewModel {
        public ButtonConfig DoPlay { get; set; }
        public ButtonConfig DoSelectSound { get; set; }
        public AlarmEntity Alarm { get; set; }

        public double Volume {
            get => (double) this.Alarm.Volume;
            set {
                this.Alarm.Volume = (decimal) value;
                LocalDbContext.Instance.ThrottleSave();
            }
        }

        public int Minutes {
            get => (int) (this.Alarm.Timer ?? 0);
            set {
                this.Alarm.Timer = value;
                LocalDbContext.Instance.ThrottleSave();
            }
        }

        public AlarmSettingsViewModel(AlarmEntity alarm) {
            this.Alarm = alarm;
            this.DoPlay = new ButtonConfig {
                Command = new CommandHandler(async () => {
                    this.DoPlay.IsEnabled = false;
                    await SoundUtil.PlayAlarm(alarm);
                    this.DoPlay.IsEnabled = true;
                }),
                Text = LocalizationContainer.Localization["Play"]
            };
            this.DoSelectSound = new ButtonConfig {
                Command = new CommandHandler(() => {
                    var openFileDialog = new OpenFileDialog();
                    openFileDialog.Multiselect = false;
                    openFileDialog.Filter = "Sound files |*.mp3; *.wav";
                    var showDialog = openFileDialog.ShowDialog();

                    if (showDialog != true) return;
                    var file = new FileInfo(openFileDialog.FileName);
                    alarm.Discriminator = file.Extension;
                    if (file.Length > 1 * 1024 * 1024) { // > 1 MB
                        alarm._Sound = new byte[0];
                        SoundUtil.AddResource(file);
                        alarm.ResourceName = file.Name;
                    }
                    else {
                        alarm._Sound = File.ReadAllBytes(file.FullName);
                    }

                    LocalDbContext.Instance.SaveChangesAsync();
                }),
                Text = LocalizationContainer.Localization["Выбрать файл"]
            };
        }
    }
}
