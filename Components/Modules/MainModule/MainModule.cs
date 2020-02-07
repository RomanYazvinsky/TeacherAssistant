using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
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

namespace TeacherAssistant.Modules.MainModule
{
    using GlobalState = ImmutableDictionary<string, object>;

    public class MainModule : SimpleModule, IDisposable
    {
        public MainModule()
            : base(typeof(WindowPageHost))
        {
        }


        public override Control GetEntryComponent()
        {
            //  Injector.Locate<MainReducer>();
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
                    new PageControllerToken());
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
            block.ExportModuleScope<ModuleActivator>();
            block.ExportModuleScope<WindowPageHost>().As<IPageHost>();
            block.ExportModuleScope<TimerService<LessonInterval, AlarmData>>();
            block.ExportFactory(() => LocalDbContext.Instance).As<LocalDbContext>().ExternallyOwned();
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
            var generalDbContext = Injector.Locate<LocalDbContext>();
            generalDbContext
                .ChangeListener<AlarmEntity>()
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ => StartTimer());
            StartTimer();
        }

        private void StartTimer()
        {
            var _db = Injector.Locate<LocalDbContext>();
            var lessonTimerService = Injector.Locate<TimerService<LessonInterval, AlarmData>>();
            var alarms = _db.Alarms
                .Where(model => model._Active > 0 && model._Timer.HasValue && model._Active == 1)
                .ToList()
                .Select(alarm => new AlarmData(alarm));

            var lessons = _db.Lessons
                .Where(model => model._Date != null
                                && model.Schedule != null
                                && model.Schedule._Begin != null
                                && model.Schedule._End != null)
                .ToList().Select(entity => new LessonInterval(entity));
            lessonTimerService.CreateSchedule(lessons, alarms);
            lessonTimerService.Start();
            var valueTuples = lessonTimerService.NextEvent;
            if (valueTuples != null)
            {
                Injector.Locate<Notifier>().ShowTimerNotification(valueTuples.Value.Item1);
            }
        }

        public void Dispose()
        {
            Application.Current.Shutdown();
        }
    }

    public class LessonInterval : IInterval
    {
        public LessonInterval(LessonEntity lesson)
        {
            Lesson = lesson;
            this.StartDateTime = lesson.Date.Value + lesson.Schedule.Begin.Value;
            this.EndDateTime = lesson.Date.Value + lesson.Schedule.End.Value;
        }

        public LessonEntity Lesson { get; }
        public DateTime StartDateTime { get; }
        public DateTime EndDateTime { get; }
    }

    public class AlarmData : IIntervalTimePoint
    {
        public AlarmData(AlarmEntity alarm)
        {
            this.Alarm = alarm;
            this.TimeDiff = alarm.SinceLessonStart.Value;
        }

        public AlarmEntity Alarm { get; }
        public StartPoint RelativelyTo { get; } = StartPoint.Start;
        public TimeSpan TimeDiff { get; }
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
