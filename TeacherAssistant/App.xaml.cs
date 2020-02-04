using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
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

namespace TeacherAssistant
{
    using GlobalState = ImmutableDictionary<string, object>;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IInjectionScope _container;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureContainer();
            LoadDatabase();
        }

        private async void ConfigureContainer()
        {
            var containerBuilder = new DependencyInjectionContainer(configuration =>
            {
                configuration.AutoRegisterUnknown = false;
                configuration.Behaviors.AllowInjectionScopeLocation = true;
                configuration.SingletonPerScopeShareContext = true;
            });
            _container = containerBuilder;
            _container.Configure(block =>
            {
                block.ExportModuleScope<ModuleActivator>();
            });
            var rootModuleLoader = _container.Locate<ModuleActivator>();
            var mainModuleToken = new MainModuleToken("TeacherAssistant");
            var mainModule = await rootModuleLoader.ActivateAsync(mainModuleToken);
            mainModule.GetEntryComponent();
        }

        private void LoadDatabase()
        {
            var defaultDatabasePath = Settings.Default.DatabasePath;
            if (File.Exists(defaultDatabasePath))
            {
                LocalDbContext.Reconnect(defaultDatabasePath);
            }
        }
    }
}
