using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Pages.GroupTablePage {
    public class GroupTableToken : PageModuleToken<GroupTableModule> {
        public GroupTableToken(string title) :
            base(title) {
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
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
