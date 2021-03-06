﻿using System.Windows.Controls;
using ReactiveUI;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.PageBase {
    public interface IView : IViewFor {
        IModuleToken ModuleToken { get; }
    }

    public abstract class View<TToken, TModel> : UserControl, IViewFor<TModel>
        where TModel : AbstractModel<TModel>
        where TToken : class, IModuleToken {
        private TToken _moduleToken;
        private TModel _viewModel;

        public IModuleToken ModuleToken {
            get => _moduleToken;
            set {
                _moduleToken = value as TToken;
                if (value == null) {
                    return;
                }

                this.Uid = value.Id;
            }
        }

        object IViewFor.ViewModel {
            get => this.ViewModel;
            set => this.ViewModel = (TModel) value;
        }

        public TModel ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                value?.Activator.Activate();
                this.DataContext = value;
            }
        }
    }
}
