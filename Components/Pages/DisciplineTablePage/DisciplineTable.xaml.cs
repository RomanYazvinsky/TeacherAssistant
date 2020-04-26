using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Pages.DisciplineTablePage {
    public class DisciplineTableToken : PageModuleToken<DisciplineTableModule> {
        public DisciplineTableToken(string title) :
            base(title) {
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class DisciplineTableModule : SimpleModule {
        public DisciplineTableModule()
            : base(typeof(DisciplineTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<DisciplineTableModel>();
            block.DeclareComponent<DisciplineTable>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class DisciplineTableBase : View<DisciplineTableToken, DisciplineTableModel> {
    }

    /// <summary>
    /// Interaction logic for DisciplineTable.xaml
    /// </summary>
    public partial class DisciplineTable : DisciplineTableBase {
        public DisciplineTable() {
            InitializeComponent();
            SortHelper.AddColumnSorting(Disciplines,
                new Dictionary<string, ListSortDirection> {{"Name", ListSortDirection.Ascending}});
        }
    }
}
