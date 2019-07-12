using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using ReactiveUI;
using Splat;

namespace TeacherAssistant {
    /// <summary>
    /// Because <class>ViewModelViewHost</class> has problems with binding ViewContract property (no property for it),
    /// this class copies all some logic of resolving content
    /// </summary>
    public class DataTemplateContentPresenter : ViewModelViewHost {
        public static DependencyProperty ViewContract2Property =
            DependencyProperty.Register("ViewContract2", typeof(string), typeof(ViewModelViewHost),
                new PropertyMetadata("", SomethingChanged));

        public static DependencyProperty ViewModel2Property =
            DependencyProperty.Register("ViewModel2", typeof(object), typeof(ViewModelViewHost),
                new PropertyMetadata(null, SomethingChanged));

        private string _viewContract;

        private readonly Subject<Unit> _updateViewModel = new Subject<Unit>();

        public DataTemplateContentPresenter() {
            var contractChanged = _updateViewModel.Select(_ => this.ViewContract2);
            var viewModelChanged = _updateViewModel.Select(_ => this.ViewModel2);

            var vmAndContract = contractChanged.CombineLatest(viewModelChanged,
                (contract, vm) => new {ViewModel = vm, Contract = contract});

            vmAndContract.Subscribe(x => ResolveViewForViewModel(x.ViewModel, x.Contract));
            contractChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => _viewContract = x);
        }

        public string ViewContract2 {
            get => (string) GetValue(ViewContract2Property);
            set => SetValue(ViewContract2Property, value);
        }

        public object ViewModel2 {
            get => GetValue(ViewModel2Property);
            set => SetValue(ViewModel2Property, value);
        }

        private static void SomethingChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
            ((DataTemplateContentPresenter) dependencyObject)._updateViewModel.OnNext(Unit.Default);
        }

        private void ResolveViewForViewModel(object viewModel, string contract) {
            if (viewModel == null) {
                this.Content = this.DefaultContent;
                return;
            }

            var viewLocator = this.ViewLocator ?? ReactiveUI.ViewLocator.Current;
            var viewInstance = viewLocator.ResolveView(viewModel, contract) ?? viewLocator.ResolveView(viewModel, null);

            if (viewInstance == null) {
                this.Content = this.DefaultContent;
                this.Log().Warn(
                    $"The {nameof(ViewModelViewHost)} could not find a valid view for the view model of type {viewModel.GetType()} and value {viewModel}.");
                return;
            }

            viewInstance.ViewModel = viewModel;

            this.Content = viewInstance;
        }
    }
}