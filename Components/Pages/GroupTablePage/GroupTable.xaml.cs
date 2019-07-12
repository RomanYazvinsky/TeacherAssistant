using System.Collections.Generic;
using System.ComponentModel;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.GroupTable
{
    /// <summary>
    /// Interaction logic for GroupTable.xaml
    /// </summary>
    public partial class GroupTable : View<GroupTableModel>
    {
        public GroupTable(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
            SortHelper.AddColumnSorting(Groups, new Dictionary<string, ListSortDirection>{{"Name", ListSortDirection.Ascending}});
        }
    }
}
