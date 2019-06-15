using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Footer
{
    public class FooterModel : AbstractModel
    {
        private ObservableCollection<StatusBarItem> _statusItems = new ObservableCollection<StatusBarItem>();

        public ObservableCollection<StatusBarItem> StatusItems
        {
            get => _statusItems;
            set
            {
                _statusItems = value;
                OnPropertyChanged(nameof(StatusItems));
            }
        }

        public override async Task Init(string id)
        {
            SimpleSubscribeCollection<StatusBarItem>("StatusItems", items =>
            {
                if (items == null) return;
                StatusItems = new ObservableCollection<StatusBarItem>(items);
            });
        }
    }
}