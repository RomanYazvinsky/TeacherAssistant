using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Model;
using Model.Models;
using Ninject;
using ReactiveUI;
using TeacherAssistant.Components;
using TeacherAssistant.Dao;
using TeacherAssistant.Pages;
using TeacherAssistant.Properties;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace TeacherAssistant {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private IKernel _container;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            ConfigureContainer();
            InitializeWindow();
            ConfigureServices();
            // Current.MainWindow?.Show();
            Exit += (sender, args) => { Injector.Get<ISerialUtil>().Close(); };
        }

        private void ConfigureContainer() {
            _container = Injector.Instance.Kernel;
            var notifier = new Notifier(configuration => {
                configuration.PositionProvider = new PrimaryScreenPositionProvider(Corner.BottomRight, 10, 10);
                configuration.LifetimeSupervisor =
                    new TimeAndCountBasedLifetimeSupervisor(
                        TimeSpan.FromMilliseconds(5000),
                        MaximumNotificationCount.FromCount(5));
                configuration.Dispatcher = Current.Dispatcher;
            });
            _container.Bind<PageService>().ToSelf().InSingletonScope();
            _container.Bind<Notifier>().ToMethod(context => notifier).InSingletonScope();
            _container.Bind<LessonTimerService>().ToSelf().InSingletonScope();
            _container.Bind<IPhotoService>().To<PhotoService>().InSingletonScope();
            _container.Bind<ISerialUtil>().To<SerialUtil>().InSingletonScope();
        }

        private string InitializeWindow() {
            var service = _container.Get<PageService>();

            var modalPageHost = new MainWindowPageHost("Modal", service);
            modalPageHost.PageAdded += (sender, control) => control?.Show();
            modalPageHost.PageClosed += (sender, control) => control?.Close();
            service.RegisterPageHost(modalPageHost);
            var openPageId = service.OpenPage("Modal", new PageProperties<MainWindowPage>());
            var window = modalPageHost.GetCurrentControl(openPageId);
            Current.MainWindow = window;
            window.Closed += (sender, args) => { Current.Shutdown(); };
            return openPageId;
        }

        private void ConfigureServices() {
            var defaultDatabasePath = Settings.Default.DatabasePath;
            if (File.Exists(defaultDatabasePath)) {
                GeneralDbContext.Reconnect(defaultDatabasePath);
            }

            var generalDbContext = GeneralDbContext.Instance;
            generalDbContext.ChangeListener<AlarmEntity>().ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { StartTimer(); });
            StartTimer();
        }

        private void StartTimer() {
            var generalDbContext = GeneralDbContext.Instance;
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