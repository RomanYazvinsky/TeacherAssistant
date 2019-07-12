using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Containers;
using EntityFramework.Rx;
using FontAwesome5;
using ReactiveUI;
using TeacherAssistant.Dao;
using Redux;
using TeacherAssistant.State;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

namespace TeacherAssistant.ComponentsImpl {
    public abstract class AbstractModel : ReactiveObject, IDisposable {
        public string Id { get; protected set; }
        protected IStore<ImmutableDictionary<string, DataContainer>> _store;
        protected PageService PageService { get; }
        protected GeneralDbContext _db;
        private BehaviorSubject<int> _refresh;
        public static bool NotNull<T>(T t) => t != null;
        public static bool NotNull<T, TV>((T, TV) t) => t.Item1 != null && t.Item2 != null;
        public static LocalizationContainer Localization { get; } = new LocalizationContainer();
        private readonly string _uniqueKey;

        protected AbstractModel(string id) {
            this.Id = id;
            this.PageService = Injector.Get<PageService>();
            _store = Storage.Instance.PublishedDataStore;
            _db = GeneralDbContext.Instance;
            _refresh = new BehaviorSubject<int>(0);
            this.Activator = new ViewModelActivator();
            _uniqueKey = "." + this.Id;
            var rus = CultureInfo.GetCultureInfo("ru-RU");
            Localization.CurrentLanguage = rus;
            StoreManager.Publish(GetControls(), this.Id, "Controls");
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

        public virtual List<ButtonConfig> GetControls() {
            return new List<ButtonConfig> {
                /*new ButtonConfig {
                    Icon = new ImageAwesome {
                        Icon = EFontAwesomeIcon.Solid_ArrowRight,
                        Width = 12,
                        Margin = new Thickness(1)
                    },
                    Tooltip = Localization["Forward"]
                },
                new ButtonConfig {
                    Icon = new ImageAwesome {
                        Icon = EFontAwesomeIcon.Solid_ArrowLeft,
                        Width = 12,
                        Margin = new Thickness(1)
                    },
                    Tooltip = Localization["Back"]
                },*/
                new ButtonConfig {
                    Icon = new ImageAwesome {
                        Icon = EFontAwesomeIcon.Solid_Redo,
                        Width = 12,
                        Margin = new Thickness(1)
                    },
                    Tooltip = Localization["Refresh"],
                    Command = new CommandHandler(Refresh)
                }
            };
        }
        
        protected IObservable<IEnumerable<DbChange<T>>> WhenDbChanges<T>() where T : class {
            return _db.ChangeListener<T>().TakeUntil(this.Activator.Deactivated);
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

        protected IObservable<T> Select<T>(params string[] idParts) {
            var id = string.Join(".", idParts);
            return ManageObservable(_store.DistinctUntilChanged(containers => containers.GetOrDefault<T>(id)))
                .Select(containers => containers.GetOrDefault<T>(id));
        }

        protected IObservable<ICollection<T>> SelectCollection<T>(string id) {
            return ManageObservable
                (
                    _store.DistinctUntilChanged(containers => containers.GetOrDefault<ICollection<T>>(id))
                )
                .Where(NotNull)
                .Select(containers => containers.GetOrDefault<ICollection<T>>(id));
        }

        protected IObservable<T> ManageObservable<T>(IObservable<T> source) {
            return source.TakeUntil(this.Activator.Deactivated).CombineLatest(_refresh, (arg1, i) => arg1);
        }

        protected abstract string GetLocalizationKey();

        protected void UpdateFromAsync(Action updateAction) {
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Background, updateAction);
        }

        public abstract Task Init();

        public bool Blocked { get; set; } = false;
        
        public virtual void Dispose() {
            _refresh.OnCompleted();
            this.Activator.Dispose();
            foreach (var keyValuePair in Localization.Where(pair => pair.Key.EndsWith(_uniqueKey))) {
                Localization.Remove(keyValuePair.Key);
            }

            new Storage.CleanupAction(this.Id).Dispatch();
        }

        public void Refresh() {
            _refresh.OnNext(0);
        }

        public ViewModelActivator Activator { get; }
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