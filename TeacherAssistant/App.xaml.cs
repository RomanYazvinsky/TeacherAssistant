using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Model;
using Model.Models;
using Ninject;
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
            var composeObjects = ComposeObjects();
            ConfigureServices(composeObjects);
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
            if (Current.Dispatcher != null) {
                Current.Dispatcher.ShutdownStarted += (sender, args) => {
                    int a = 5;
                };
            }

            _container.Bind<PageService>().ToSelf().InSingletonScope();
            _container.Bind<Notifier>().ToMethod(context => notifier).InSingletonScope();
            _container.Bind<LessonTimerService>().ToSelf().InSingletonScope();
            _container.Bind<SoundService>().ToSelf().InSingletonScope();
            _container.Bind<IPhotoService>().To<PhotoService>().InSingletonScope();
            _container.Bind<ISerialUtil>().To<SerialUtil>().InSingletonScope();
        }

        private string ComposeObjects() {
            var service = _container.Get<PageService>();

            var modalPageHost = new MainWindowPageHost("Modal", service);
            modalPageHost.PageAdded += (sender, control) => control?.Show();
            modalPageHost.PageClosed += (sender, control) => control?.Close();
            service.RegisterPageHost(modalPageHost);
            var openPageId = service.OpenPage("Modal", new PageProperties {PageType = typeof(MainWindowPage)});
            var window = modalPageHost.GetCurrentControl(openPageId);
            Current.MainWindow = window;
            window.Closed += (sender, args) => { Current.Shutdown(); };
            return openPageId;
        }

        private void ConfigureServices(string notificationArea) {
            var soundService = _container.Get<SoundService>();
            var defaultDatabasePath = Settings.Default.DatabasePath;
            if (File.Exists(defaultDatabasePath)) {
                GeneralDbContext.Reconnect(defaultDatabasePath);
            }

            var player = new MediaPlayer();
            var generalDbContext = GeneralDbContext.Instance;
            generalDbContext.ChangeListener<AlarmModel>().Subscribe(changes => {
                soundService.InitLibrary(generalDbContext.AlarmModels.ToList());
            });
            generalDbContext.ChangeListener<AlarmModel>().CombineLatest(generalDbContext.ChangeListener<LessonModel>(),
                    (changes, enumerable) => "")
                .Subscribe(_ => { StartTimer(soundService, player, notificationArea); });
            StartTimer(soundService, player, notificationArea);
        }

        private void StartTimer(SoundService service, MediaPlayer player, string area) {
            var generalDbContext = GeneralDbContext.Instance;
            var lessonTimerService = _container.Get<LessonTimerService>();
            var alarms = generalDbContext.AlarmModels.Where(model => model._Active > 0 && model.Timer.HasValue)
                .ToDictionary(model => TimeSpan.FromMinutes(model.Timer.Value),
                    model => {
                        return new Action<LessonModel>((lessonModel => {
                            player.Open(service.GetSoundPath(model));
                            player.Volume = (double) model.Volume % 10d;
                            player.Play();
                        }));
                    });

            lessonTimerService.Init(generalDbContext.LessonModels.Where(model => model._Date != null).ToList(),
                alarms);
            var dateTime = lessonTimerService.Start();
            if (dateTime != null) {
                _container.Get<Notifier>().ShowTimerNotification(dateTime.Value);
            }
        }
    }
}