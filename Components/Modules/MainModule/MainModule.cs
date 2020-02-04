using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Grace.DependencyInjection;
using Model;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.Core.Effects;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.State;
using TeacherAssistant.Dao;
using TeacherAssistant.Pages;
using TeacherAssistant.ReaderPlugin;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace TeacherAssistant.Modules.MainModule {
    using GlobalState = ImmutableDictionary<string, object>;

    public class MainModule : SimpleModule {

        public MainModule()
            : base(typeof(WindowPageHost)) {
        }


        public override Control GetEntryComponent()
        {
          //  Injector.Locate<MainReducer>();
            var windowPageHost = this.Injector?.Locate<WindowPageHost>();
            if (windowPageHost == null) {
                return null;
            }

            var pages = windowPageHost.CurrentPages.ToList();
            if (pages.Any()) {
                return pages.First();
            }
            ConfigureServices();
            windowPageHost
                .AddPageAsync<PageControllerModule, PageControllerToken>(
                    new PageControllerToken());
            return null;
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<SimpleEffectsMiddleware<GlobalState>>();
            block.ExportModuleScope<Storage>();
            block.ExportModuleScope<MainReducer>();
            block.ExportModuleScope<SerialUtil>();
            block.ExportModuleScope<StudentCardService>();
            block.ExportModuleScope<PhotoService>();
            block.ExportModuleScope<ModuleActivator>();
            block.ExportModuleScope<WindowPageHost>();
            block.ExportModuleScope<LessonTimerService>();
            block.ExportFactory(() => LocalDbContext.Instance).As<LocalDbContext>().ExternallyOwned();
            var notifier = new Notifier(configuration => {
                configuration.PositionProvider = new PrimaryScreenPositionProvider(Corner.BottomRight, 10, 10);
                configuration.LifetimeSupervisor =
                    new TimeAndCountBasedLifetimeSupervisor(
                        TimeSpan.FromMilliseconds(5000),
                        MaximumNotificationCount.FromCount(5));
                configuration.Dispatcher = Application.Current.Dispatcher;
            });
            block.ExportInstance(notifier);
        }

        private void ConfigureServices()
        {
            var generalDbContext = Injector.Locate<LocalDbContext>();
            generalDbContext
                .ChangeListener<AlarmEntity>()
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ => StartTimer());
            StartTimer();
        }

        private void StartTimer()
        {
            var generalDbContext = Injector.Locate<LocalDbContext>();
            var lessonTimerService = Injector.Locate<LessonTimerService>();
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
            if (dateTime != null)
            {
                Injector.Locate<Notifier>().ShowTimerNotification(dateTime.Value);
            }
        }
    }

    public class SetFullscreenModeAction : ModuleScopeAction {
        public bool? Fullscreen { get; }

        public SetFullscreenModeAction(bool? fullscreen = null) {
            this.Fullscreen = fullscreen;
        }
    }
}
