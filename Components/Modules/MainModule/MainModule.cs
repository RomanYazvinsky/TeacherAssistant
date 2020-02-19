using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Containers;
using EntityFramework.Rx;
using EntityFramework.Triggers;
using Grace.DependencyInjection;
using Model;
using Model.Models;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.Core.Effects;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.State;
using TeacherAssistant.Database;
using TeacherAssistant.Notifications;
using TeacherAssistant.PageHostProviders;
using TeacherAssistant.Pages;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.Services;
using TeacherAssistant.Services.Paging;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace TeacherAssistant.Modules.MainModule
{
    using GlobalState = ImmutableDictionary<string, object>;

    public class MainModule : SimpleModule, IDisposable
    {
        private readonly Subject<Unit> _destroySubject = new Subject<Unit>();

        public MainModule()
            : base(typeof(WindowPageHost))
        {
        }


        public override Control GetEntryComponent()
        {
            var windowPageHost = this.Injector?.Locate<WindowPageHost>();
            if (windowPageHost == null)
            {
                return null;
            }

            var pages = windowPageHost.CurrentPages.ToList();
            if (pages.Any())
            {
                return pages.First();
            }

            ConfigureServices();
            windowPageHost
                .AddPageAsync<PageControllerModule, PageControllerToken>(
                    new PageControllerToken(new ScheduleToken("Расписание")));
            return null;
        }

        public override void Configure(IExportRegistrationBlock block)
        {
            block.ExportModuleScope<SimpleEffectsMiddleware<GlobalState>>();
            block.ExportModuleScope<Storage>();
            block.ExportModuleScope<MainReducer>();
            block.ExportModuleScope<SerialUtil>();
            block.ExportModuleScope<StudentCardService>();
            block.ExportModuleScope<PhotoService>();
            block.ExportModuleScope<AudioService>();
            block.ExportModuleScope<ModuleActivator>();
            block.ExportModuleScope<WindowPageHost>().As<IPageHost>();
            block.ExportModuleScope<TimerService<LessonInterval, AlarmEvent>>();
            block.ExportModuleScope<DatabaseBackupService>();
            var notifier = new Notifier(configuration =>
            {
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
            DbObservable<AlarmEntity, LocalDbContext>.FromInserted()
                .Merge<IEntry>(DbObservable<AlarmEntity, LocalDbContext>.FromDeleted())
                .Merge(DbObservable<AlarmEntity, LocalDbContext>.FromUpdated())
                .Cast<object>()
                .Merge(DbObservable<LessonEntity, LocalDbContext>.FromInserted())
                .Merge(DbObservable<LessonEntity, LocalDbContext>.FromUpdated())
                .Merge(DbObservable<LessonEntity, LocalDbContext>.FromDeleted())
                .TakeUntil(_destroySubject)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ => StartTimer());
            StartTimer();
            StartBackupService();
        }

        private void StartBackupService()
        {
            var databaseBackupService = this.Injector.Locate<DatabaseBackupService>();
            databaseBackupService.BackupDatabase();
            Observable.Interval(TimeSpan.FromMinutes(5))
                .TakeUntil(_destroySubject)
                .Subscribe(_ => databaseBackupService.BackupDatabase());
        }

        private void StartTimer()
        {
            var db = Injector.Locate<LocalDbContext>();
            var lessonTimerService = Injector.Locate<TimerService<LessonInterval, AlarmEvent>>();
            var util = Injector.Locate<AudioService>();
            var alarms = db.Alarms
                .Where(model => model._Active > 0 && model._Timer.HasValue && model._Active == 1)
                .ToList()
                .Select(alarm => new AlarmEvent(alarm));
            var now = DateTime.Now;
            var lessons = db.Lessons
                .Where(model => model._Date != null
                                && model.Schedule != null
                                && model.Schedule._Begin != null
                                && model.Schedule._End != null)
                .ToList()
                .Where(entity => entity.Date?.Date >= now.Date)
                .Select(entity => new LessonInterval(entity));
            lessonTimerService.CreateSchedule(lessons, alarms);
            lessonTimerService.OnScheduled.Subscribe(async list =>
            {
                foreach (var tuple in list)
                {
                    var (lessonInterval, alarmEvent) = tuple;
                    await util.PlayAlarm(alarmEvent.Alarm);
                }
            });
            lessonTimerService.Start();
            var nextStarts = lessonTimerService.NextStarts;
            if (nextStarts != null)
            {
                Injector.Locate<Notifier>().ShowTimerNotification(nextStarts.First().StartDateTime);
            }
        }

        public void Dispose()
        {
            _destroySubject.OnNext(Unit.Default);
            _destroySubject.Dispose();
            Application.Current.Shutdown();
        }
    }

    public class SetFullscreenModeAction : ModuleScopeAction
    {
        public bool? Fullscreen { get; }

        public SetFullscreenModeAction(bool? fullscreen = null)
        {
            this.Fullscreen = fullscreen;
        }
    }
}
