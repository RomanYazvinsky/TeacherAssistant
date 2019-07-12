using System;
using ReactiveUI;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl {
    public class View<T> : ReactiveUserControl<T> where T : AbstractModel {

        protected event EventHandler<T> ViewModelLoaded; 
        public void InitializeViewModel(string id) {
            this.Uid = id;
//            Loaded += (sender, args) => {
                this.ViewModel = Injector.Get<T>
                (
                    ("id", this.Uid)
                );
                this.DataContext = this.ViewModel;
                this.ViewModel.Init();
                ViewModelLoaded?.Invoke(this, this.ViewModel);
//            };
            MouseDown += (sender, args) => { args.Handled = !this.ViewModel.Blocked; };
            Unloaded += (sender, args) => this.ViewModel?.Dispose();
        }
    }
}