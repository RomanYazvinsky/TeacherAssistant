using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Windows;
using Grace.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Database;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.Properties;

namespace TeacherAssistant {
    using GlobalState = ImmutableDictionary<string, object>;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private IInjectionScope _container;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            ConfigureLogger();
            ConfigureContainer();
        }

        private async void ConfigureContainer() {
            var containerBuilder = new DependencyInjectionContainer(configuration => {
                configuration.AutoRegisterUnknown = false;
                configuration.Behaviors.AllowInjectionScopeLocation = true;
                configuration.SingletonPerScopeShareContext = true;
            });
            _container = containerBuilder;
            _container.Configure(block => {
                block.AddModule(new DatabaseModule());
                block.ExportModuleScope<ModuleActivator>();
            });
            var isConnected = await ConnectDatabase(_container, Settings.Default.DatabasePath);
            if (!isConnected) {
                var isConnectedToNew = CreateDatabase(_container);
                if (!isConnectedToNew) {
                    throw new Exception("Cannot create new database!");
                }
            }
            var rootModuleLoader = _container.Locate<ModuleActivator>();
            var mainModuleToken = new MainModuleToken("TeacherAssistant");
            var mainModule = await rootModuleLoader.ActivateAsync(mainModuleToken);
            mainModule.GetEntryComponent();
        }

        private async Task<bool> ConnectDatabase(ILocatorService scope, string path) {
            var databaseManager = scope.Locate<DatabaseManager>();
            try {
                await databaseManager.Connect(path);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        private bool CreateDatabase(ILocatorService scope) {
            var databaseManager = scope.Locate<DatabaseManager>();
            try {
                databaseManager.CreateAndConnectNewDatabase("./db.s3db");
                return true;
            }
            catch (Exception e) {
                Logger.Log(LogLevel.Error, e);
                return false;
            }
        }

        private void ConfigureLogger() {
            var config = new LoggingConfiguration();

            var logfile = new FileTarget("logfile") {FileName = $"logs/log-{DateTime.Now:G}.log"};
            var logconsole = new ConsoleTarget("logconsole");

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;
        }
    }
}
