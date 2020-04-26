using System.Windows.Controls;
using ReactiveUI;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.PageBase
{
    public abstract class View<TToken, TModel> : UserControl, IViewFor<TModel>
        where TModel : AbstractModel<TModel>
        where TToken : class, IModuleToken
    {
        private ModuleActivation<TToken> _moduleActivation;
        private TModel _viewModel;

        public ModuleActivation<TToken> ModuleToken
        {
            get => _moduleActivation;
            set
            {
                _moduleActivation = value;
                if (value == null)
                {
                    return;
                }

                this.Uid = value.Id;
            }
        }

        object IViewFor.ViewModel
        {
            get => this.ViewModel;
            set => this.ViewModel = (TModel) value;
        }

        public TModel ViewModel
        {
            get => _viewModel;
            set
            {
                _viewModel = value;
                value?.Activator.Activate();
                this.DataContext = value;
            }
        }
    }
}