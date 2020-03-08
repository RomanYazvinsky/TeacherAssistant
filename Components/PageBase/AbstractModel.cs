using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Containers;
using EntityFramework.Rx;
using FontAwesome5;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Database;
using TeacherAssistant.State;

namespace TeacherAssistant.PageBase {

    public abstract class AbstractModel<TSelf> : ReactiveValidationObject<TSelf>, IDisposable, IActivatableViewModel
    {
        private const int BufferizationTime = 300;

        public static LocalizationContainer Localization { get; } = LocalizationContainer.Localization;
        protected Subject<Unit> DestroySubject { get; } = new Subject<Unit>();

        public ViewModelActivator Activator { get; }
        private readonly string _uniqueKey = IdGenerator.GenerateId();


        protected AbstractModel() {
            this.Activator = new ViewModelActivator();
            var rus = CultureInfo.GetCultureInfo("ru-RU");
            Localization.CurrentLanguage = rus;
        }

        public void InterpolateLocalization(string key, params object[] values) {
            this[key] = LocalizationContainer.Interpolate(key, values);
        }


        [IndexerName("Item")]
        public string this[string key] {
            get => Localization[key + _uniqueKey];
            set {
                Localization[key + _uniqueKey] = value;
                this.RaisePropertyChanged("Item[]");
            }
        }

        protected IObservable<IEnumerable<T>> WhenAdded<T>() where T : class
        {
            var observable = DbObservable<T, LocalDbContext>.FromInserted()
                .TakeUntil(this.Activator.Deactivated)
                .Select(entry => entry.Entity);
            var throttle = observable.Throttle(TimeSpan.FromMilliseconds(BufferizationTime));
            return observable.Buffer(throttle);
        }

        protected IObservable<IEnumerable<T>> WhenRemoved<T>() where T : class {
            var observable = DbObservable<T, LocalDbContext>.FromDeleted()
                .TakeUntil(this.Activator.Deactivated)
                .Select(entry => entry.Entity);
            var throttle = observable.Throttle(TimeSpan.FromMilliseconds(BufferizationTime));
            return observable.Buffer(throttle);
        }

        protected IObservable<IEnumerable<T>> WhenUpdated<T>() where T : class {
            var observable = DbObservable<T, LocalDbContext>.FromUpdated()
                .TakeUntil(this.Activator.Deactivated)
                .Select(entry => entry.Entity);
            var throttle = observable.Throttle(TimeSpan.FromMilliseconds(BufferizationTime));
            return observable.Buffer(throttle);
        }


        protected abstract string GetLocalizationKey();

        protected Task RunInUiThread(Action updateAction) {
            if (Thread.CurrentThread.ManagedThreadId.Equals(Application.Current?.Dispatcher?.Thread.ManagedThreadId))
            {
                updateAction();
                return Task.CompletedTask;
            }
            return Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Background, updateAction).Task;
        }

        public virtual void Dispose() {
            this.DestroySubject.OnNext(Unit.Default);
            this.DestroySubject.Dispose();
            this.Activator.Dispose();
            foreach (var keyValuePair in Localization.Where(pair => pair.Key.EndsWith(_uniqueKey))) {
                Localization.Remove(keyValuePair.Key);
            }
        }

    }

    public static class DepExt {
        public static T GetChildOfType<T>(this DependencyObject depObj)
            where T : DependencyObject {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }

            return null;
        }
    }
}
