using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Grace.DependencyInjection;
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


        public override Control GetEntryComponent() {
            var windowPageHost = this.Injector?.Locate<WindowPageHost>();
            if (windowPageHost == null) {
                return null;
            }

            var pages = windowPageHost.CurrentPages.ToList();
            if (pages.Any()) {
                return pages.First();
            }

            return windowPageHost
                .AddPage<PageControllerModule, PageControllerToken>(
                    new PageControllerToken())
                .GetEntryComponent();
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<MainReducer>(this.ModuleToken.Id);
            block.ExportModuleScope<SerialUtil>(this.ModuleToken.Id);
            block.ExportModuleScope<PhotoService>(this.ModuleToken.Id);
            block.ExportModuleScope<ModuleLoader>(this.ModuleToken.Id);
            block.ExportModuleScope<WindowPageHost>(this.ModuleToken.Id);
            block.ExportModuleScope<LessonTimerService>(this.ModuleToken.Id);
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
    }

    public class SetFullscreenModeAction : ModuleScopeAction {
        public bool? Fullscreen { get; }

        public SetFullscreenModeAction(bool? fullscreen = null) {
            this.Fullscreen = fullscreen;
        }
    }
}