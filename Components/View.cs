using System.Windows;
using Ninject;
using ReactiveUI;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.ComponentsImpl {
    public class View<T> : ReactiveUserControl<T>, IInitializable where T : AbstractModel {

        [Inject]
        public void SetModuleActivationToken(IModuleToken token) {
            void Handler(object sender, RoutedEventArgs args) {
                token.Deactivate();
                Unloaded -= Handler;
            }

            Unloaded += Handler;
            this.Uid = token.Id;
        }

        [Inject]
        public void SetViewModel(T model) {
            this.ViewModel = model;
            this.DataContext = model;
        }

        public virtual void Initialize() {
        }
    }
}