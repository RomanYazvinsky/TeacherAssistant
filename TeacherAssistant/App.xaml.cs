using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Lifestyle;
using Model;
using Model.Models;
using ReactiveUI;
using TeacherAssistant.Core.Effects;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.State;
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
        private IInjectionScope _container;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            ConfigureContainer();
            // ConfigureServices();
        }

        private void ConfigureContainer() {
            var containerBuilder = new DependencyInjectionContainer();
            _container = containerBuilder;
            _container.Configure(block => {
                block.Export<ModuleLoader>().Lifestyle.SingletonPerNamedScope("RootScope");
                block.ExportModuleScope<Storage>("RootScope");
                block.ExportModuleScope<SimpleEffectsMiddleware<GlobalState>>("RootScope");
            });
            var rootModuleLoader = _container.Locate<ModuleLoader>();
            var mainModuleToken = new MainModuleToken("TeacherAssistant") {ExitOnClose = true};
            var mainModule = rootModuleLoader.Activate(mainModuleToken);
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
            var lessonTimerService = _container.Locate<LessonTimerService>();
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
                _container.Locate<Notifier>().ShowTimerNotification(dateTime.Value);
            }
        }
    }
}