using System.Collections;
using System.Collections.ObjectModel;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Components.Table
{
    public class TableModel<T> : AbstractModel
    {
        private ObservableCollection<T> _items;
        private IList _selectedItems = new ObservableCollection<T>();
        private T _selectedItem;

        public TableModel(string id) : base(id)
        {
            S<ObservableCollection<T>>(id + ".Items", models =>
            {
                Items = models;
            });
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

        public IList SelectedItems
        {
            get => _selectedItems;
            set
            {
                _selectedItems = value;
                Publisher.Publish(_id + ".SelectedItems", _selectedItems);
                OnPropertyChanged(nameof(SelectedItems));
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