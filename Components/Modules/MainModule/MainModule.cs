using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ninject;
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

    public class MainModule : Module {
        private readonly MainModuleToken _token;

        public MainModule(MainModuleToken token)
            : base(
                new[] {
                    typeof(WindowPageHost),
                    typeof(MainReducer),
                    typeof(SerialUtil),
                    typeof(PhotoService),
                    typeof(Storage),
                    typeof(SimpleEffectsMiddleware<GlobalState>),
                    typeof(LessonTimerService),
                }) {
            _token = token;
        }

        public override void Load() {
            base.Load();
            var notifier = new Notifier(configuration => {
                configuration.PositionProvider = new PrimaryScreenPositionProvider(Corner.BottomRight, 10, 10);
                configuration.LifetimeSupervisor =
                    new TimeAndCountBasedLifetimeSupervisor(
                        TimeSpan.FromMilliseconds(5000),
                        MaximumNotificationCount.FromCount(5));
                configuration.Dispatcher = Application.Current.Dispatcher;
            });
            Bind<Notifier>().ToMethod(context => notifier).InSingletonScope();
            Bind<LocalDbContext>().ToConstant(LocalDbContext.Instance)
                .InSingletonScope();
        }

        public override Control GetEntryComponent() {
            var windowPageHost = this.Kernel?.Get<WindowPageHost>();
            if (windowPageHost == null) {
                return null;
            }

            var pages = windowPageHost.CurrentPages.ToList();
            if (pages.Any()) {
                return pages.First();
            }

            return windowPageHost
                .AddPage<PageControllerModule, PageControllerToken>(
                    new PageControllerToken(_token.Title))
                .GetEntryComponent();
        }
    }

    public class SetFullscreenModeAction : ModuleScopeAction {
        public bool Fullscreen { get; }

        protected SetFullscreenModeAction(bool fullscreen) {
            this.Fullscreen = fullscreen;
        }
    }
}