using System.Windows;
using Ninject;
using Ninject.Extensions.Conventions;
using TeacherAssistant.Components;
using TeacherAssistant.GroupTable;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.RegistrationPage;
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
            container.Bind(syntax => syntax.FromAssembliesMatching(".").SelectAllClasses()
                .InheritedFrom<ISerialUtil>().BindDefaultInterface().Configure(onSyntax => onSyntax.InSingletonScope()));
            container.Bind(syntax => syntax.FromAssembliesMatching(".").SelectAllClasses()
                .InheritedFrom<IPhotoService>().BindDefaultInterface().Configure(onSyntax => onSyntax.InSingletonScope()));
        }

        private void ComposeObjects()
        {
            Current.MainWindow = container.Get<MainWindow>();
        }
    }
}
