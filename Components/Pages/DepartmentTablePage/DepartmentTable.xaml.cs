using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Pages.DepartmentTablePage {
    public class DepartmentTableToken : PageModuleToken<DepartmentTableModule> {
        public DepartmentTableToken(string title) :
            base(title) {
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class DepartmentTableModule : SimpleModule {
        public DepartmentTableModule()
            : base(typeof(DepartmentTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<DepartmentTableModel>();
            block.DeclareComponent<DepartmentTable>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class DepartmentTableBase : View<DepartmentTableToken, DepartmentTableModel> {
    }

    /// <summary>
    /// Interaction logic for DepartmentTable.xaml
    /// </summary>
    public partial class DepartmentTable : DepartmentTableBase {
        public DepartmentTable() {
            InitializeComponent();
            SortHelper.AddColumnSorting(Departments,
                new Dictionary<string, ListSortDirection> {{"Name", ListSortDirection.Ascending}});
        }
    }
}
