using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Containers;
using EntityFramework.Rx;
using FontAwesome5;
using ReactiveUI;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl {

    public abstract class AbstractModel : ReactiveObject, IDisposable, IActivatableViewModel {

        public static bool NotNull<T>(T t) => t != null;
        public static bool NotNull<T, TV>((T, TV) t) => t.Item1 != null && t.Item2 != null;
        public static Tuple<T1, T2> ToTuple<T1, T2>(T1 t1, T2 t2) => new Tuple<T1, T2>(t1, t2);

        public static LocalizationContainer Localization { get; } = new LocalizationContainer();
        protected BehaviorSubject<int> RefreshSubject { get; } = new BehaviorSubject<int>(0);

        public ViewModelActivator Activator { get; }
        private readonly string _uniqueKey = IdGenerator.GenerateId();


        protected AbstractModel() {
            this.Activator = new ViewModelActivator();
            var rus = CultureInfo.GetCultureInfo("ru-RU");
            Localization.CurrentLanguage = rus;
        }

        public void InterpolateLocalization(string key, params object[] values) {
            this[key] = Interpolate(key, values);
        }

        public static string Interpolate(string key, params object[] values) {
            return string.Format(Localization[key], values);
        }

        [IndexerName("Item")]
        public string this[string key] {
            get => Localization[key + _uniqueKey];
            set {
                Localization[key + _uniqueKey] = value;
                this.RaisePropertyChanged("Item[]");
            }
        }

        protected ButtonConfig GetRefreshButtonConfig() {
            return new ButtonConfig {
                Icon = new ImageAwesome {
                    Icon = EFontAwesomeIcon.Solid_Redo,
                    Width = 12,
                    Margin = new Thickness(1)
                },
                Tooltip = Localization["Refresh"],
                Command = new CommandHandler(Refresh)
            };
        }

        protected IObservable<IEnumerable<T>> WhenAdded<T>() where T : class {
            return DbObservable.FromInserted<T>()
                .TakeUntil(this.Activator.Deactivated)
                .Select(entry => entry.Entity)
                .Buffer(TimeSpan.FromMilliseconds(500));
        }

        protected IObservable<IEnumerable<T>> WhenRemoved<T>() where T : class {
            return DbObservable.FromDeleted<T>()
                .TakeUntil(this.Activator.Deactivated)
                .Select(entry => entry.Entity)
                .Buffer(TimeSpan.FromMilliseconds(500));
        }

        protected IObservable<IEnumerable<T>> WhenUpdated<T>() where T : class {
            return DbObservable.FromUpdated<T>()
                .TakeUntil(this.Activator.Deactivated)
                .Select(entry => entry.Entity)
                .Buffer(TimeSpan.FromMilliseconds(500));
        }

        private Dictionary<string, string> BuildPageLocalization(LocalizationContainer localization, string pageName) {
            return localization.Where(pair => pair.Key.StartsWith(pageName) || pair.Key.StartsWith("common."))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        protected IObservable<T> ManageObservable<T>(IObservable<T> source) {
            return source.TakeUntil(this.Activator.Deactivated)
                .CombineLatest(RefreshSubject, (arg1, i) => arg1);
        }


        protected abstract string GetLocalizationKey();

        protected void RunInUiThread(Action updateAction) {
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Background, updateAction);
        }

        public bool Blocked { get; set; } = false;

        public virtual void Dispose() {
            RefreshSubject.OnCompleted();
            this.Activator.Dispose();
            foreach (var keyValuePair in Localization.Where(pair => pair.Key.EndsWith(_uniqueKey))) {
                Localization.Remove(keyValuePair.Key);
            }
        }

        public void Refresh() {
            RefreshSubject.OnNext(0);
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
