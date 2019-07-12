using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Containers.Annotations;

namespace TeacherAssistant.ComponentsImpl
{
    public abstract class SafeDictionary<T> : IEnumerable<KeyValuePair<string, T>>, INotifyPropertyChanged
    {

        private readonly Dictionary<string, T> _container = new Dictionary<string, T>();

        [IndexerName("Item")]
        public T this[string key]
        {
            get => _container.TryGetValue(key, out var value) ? value : GetDefault(key);
            set
            {
                if (_container.ContainsKey(key))
                {
                    _container[key] = value;
                }
                else
                {
                    Add(key, value);
                }
                OnPropertyChanged("Item[]");
            }
        }

        public abstract T GetDefault(string key);

        public void Add(string key, T value)
        {
            _container.Add(key, value);
            OnPropertyChanged(nameof(key));
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return _container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}