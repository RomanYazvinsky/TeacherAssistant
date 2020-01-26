using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Model;
using Model.Models;
using Ninject;
using ReactiveUI;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Dao;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.Properties;
using ToastNotifications;

namespace TeacherAssistant {
    using GlobalState = ImmutableDictionary<string, object>;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private IKernel _container;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            ConfigureContainer();
            // ConfigureServices();
        }

        private void ConfigureContainer() {
            _container = new StandardKernel();
            _container.Bind<ModuleLoader>().ToSelf().InSingletonScope();
            var rootModuleLoader = _container.Get<ModuleLoader>();
            var mainModuleToken = new MainModuleToken("TeacherAssistant") {ExitOnClose = true};
            var mainModule = rootModuleLoader.Activate<MainModule, MainModuleToken>(mainModuleToken);
            mainModule.GetEntryComponent();
        }


        private void ConfigureServices() {
            var defaultDatabasePath = Settings.Default.DatabasePath;
            if (File.Exists(defaultDatabasePath)) {
                LocalDbContext.Reconnect(defaultDatabasePath);
            }

            var generalDbContext = LocalDbContext.Instance;
            generalDbContext.ChangeListener<AlarmEntity>().ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { StartTimer(); });
            StartTimer();
        }

        private void StartTimer() {
            var generalDbContext = LocalDbContext.Instance;
            var lessonTimerService = _container.Get<LessonTimerService>();
            var alarms = generalDbContext.Alarms
                .Where(model => model._Active > 0 && model.Timer.HasValue && model._Active == 1)
                .ToList()
                .GroupBy(entity => entity.Timer.Value) // if one or more in the same time - select only first
                .Select(entities => entities.FirstOrDefault())
                .ToDictionary(
                    alarm => TimeSpan.FromMinutes(alarm.Timer.Value),
                    model => new Action<LessonEntity>(lessonModel => SoundUtil.PlayAlarm(model))
                );

            lessonTimerService.Init(
                generalDbContext.Lessons.Where(model => model._Date != null).ToList(),
                alarms
            );
            var dateTime = lessonTimerService.Start();
            if (dateTime != null) {
                _container.Get<Notifier>().ShowTimerNotification(dateTime.Value);
            }
        }
    }
}