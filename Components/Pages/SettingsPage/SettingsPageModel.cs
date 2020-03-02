using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Containers;
using DynamicData;
using Microsoft.Win32;
using ReactiveUI;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services;

namespace TeacherAssistant.Pages.SettingsPage {
    public class SettingsPageModel : AbstractModel<SettingsPageModel> {
        public SettingsPageModel(AudioService service, LocalDbContext context) {
            this.Alarms.Clear();
            this.Alarms.AddRange(context.Alarms
                .Select(alarm => new AlarmSettingsViewModel(alarm, service, context)).ToList());
        }

        public ObservableCollection<AlarmSettingsViewModel> Alarms { get; set; } =
            new ObservableCollection<AlarmSettingsViewModel>();

        protected override string GetLocalizationKey() {
            return "settings";
        }
    }

    public class AlarmSettingsViewModel {
        private readonly LocalDbContext _context;
        public ButtonConfig DoPlay { get; set; }
        public ButtonConfig DoSelectSound { get; set; }
        public AlarmEntity Alarm { get; set; }

        public double Volume {
            get => (double) this.Alarm.Volume;
            set {
                this.Alarm.Volume = (decimal) value;
                _context.ThrottleSave();
            }
        }

        public int Minutes {
            get => (int) (this.Alarm._Timer ?? 0);
            set {
                this.Alarm._Timer = value;
                _context.ThrottleSave();
            }
        }

        public AlarmSettingsViewModel(AlarmEntity alarm, AudioService service, LocalDbContext context) {
            _context = context;
            this.Alarm = alarm;
            this.DoPlay = new ButtonConfig {
                Command = ReactiveCommand.Create(async () => {
                    this.DoPlay.IsEnabled = false;
                    await service.PlayAlarm(alarm);
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
                        alarm.Sound = new byte[0];
                        service.AddResource(file);
                        alarm.ResourceName = file.Name;
                    }
                    else {
                        alarm.Sound = File.ReadAllBytes(file.FullName);
                    }

                    context.SaveChangesAsync();
                }),
                Text = LocalizationContainer.Localization["Выбрать файл"]
            };
        }
    }
}