using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.StudentTable {
    public class StudentTableToken : PageModuleToken<StudentTableModule> {
        public StudentTableToken(string title) : base(title) {
        }
    }

    public class StudentTableModule : SimpleModule {
        public StudentTableModule() : base(typeof(StudentTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<StudentTableModel>(this.ModuleToken.Id);
            block.ExportModuleScope<StudentTable>(this.ModuleToken.Id)
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