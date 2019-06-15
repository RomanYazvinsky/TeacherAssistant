using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Windows.Data;
using Dao;
using Model.Models;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.GroupTable
{
    public class GroupTableModel : AbstractModel
    {
        private GroupModel _selectedGroupModel;
        private ListCollectionView _groups = new ListCollectionView(new List<GroupModel>());
        private ObservableCollection<GroupModel> _groupModels = new ObservableCollection<GroupModel>();

        public override async Task Init(string id)
        {
            _groupModels = new ObservableCollection<GroupModel>(GeneralDbContext.Instance.GroupModels
                .Include(model => model.Department).Include(model => model.Praepostor));
            _groups = new ListCollectionView(_groupModels);
        }

        public ListCollectionView Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }

        public GroupModel SelectedGroupModel
        {
            get => _selectedGroupModel;
            set
            {
                _selectedGroupModel = value;
                OnPropertyChanged(nameof(SelectedGroupModel));
            }
        }
    }
}