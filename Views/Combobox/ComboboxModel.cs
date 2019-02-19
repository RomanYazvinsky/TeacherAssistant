using System.Collections.ObjectModel;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Components.Combobox
{
    public class ComboboxModel<T> : AbstractModel
    {
        private ObservableCollection<T> _items;
        private T _selectedItem;

        public ComboboxModel(string id) : base(id)
        {
            S<ObservableCollection<T>>(id + ".Items", obj => { Items = obj; });
        }

        public ObservableCollection<T> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public T SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                Publisher.Publish(_id + ".SelectedItem", _selectedItem);
                OnPropertyChanged(nameof(SelectedItem));
            }
        }
    }
}