using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Dao;
using Ninject;
using Ninject.Extensions.Conventions;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;

namespace TeacherAssistant
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IKernel container;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureContainer();
            ComposeObjects();
            Current.MainWindow.Show();
        }

        private void ConfigureContainer()
        {
            container = Injector.GetInstance().Kernel;
        }

        private void ComposeObjects()
        {
            Current.MainWindow = container.Get<MainWindow>();
        }
    }
}
