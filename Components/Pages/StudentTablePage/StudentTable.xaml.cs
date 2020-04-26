using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.StudentTablePage;

namespace TeacherAssistant.StudentTable {
    public class StudentTableToken : PageModuleToken<StudentTableModule> {
        public StudentTableToken(string title) : base(title) {
        }

        public override PageProperties PageProperties { get; }= new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class StudentTableModule : SimpleModule {
        public StudentTableModule() : base(typeof(StudentTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<StudentTableModel>();
            block.DeclareComponent<StudentTable>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel)
                ;
        }
    }

    public class StudentTableBase : View<StudentTableToken, StudentTableModel> {
    }

    /// <summary>
    /// Interaction logic for StudentTable.xaml
    /// </summary>
    public partial class StudentTable : StudentTableBase {
        public StudentTable() {
            InitializeComponent();
        }
    }
}
