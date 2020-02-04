using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.GroupTable {
    public class GroupTableToken : PageModuleToken<GroupTableModule> {
        public GroupTableToken(string title) :
            base(title) {
        }

    }

    public class GroupTableModule : SimpleModule {
        public GroupTableModule()
            : base(typeof(GroupTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<GroupTableModel>();
            block.ExportModuleScope<GroupTable>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class GroupTableBase : View<GroupTableToken, GroupTableModel> {
    }

    /// <summary>
    /// Interaction logic for GroupTable.xaml
    /// </summary>
    public partial class GroupTable : GroupTableBase {
        public GroupTable() {
            InitializeComponent();
            SortHelper.AddColumnSorting(Groups,
                new Dictionary<string, ListSortDirection> {{"Name", ListSortDirection.Ascending}});
        }
    }
}
