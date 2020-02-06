using Containers.Annotations;
using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.StudentForm {
    public class StudentFormToken : PageModuleToken<StudentFormModule> {
        public StudentFormToken(string title, [NotNull] StudentEntity student) :
            base(title) {
            this.Student = student;
        }

        [NotNull] public StudentEntity Student { get; }
    }

    public class StudentFormModule : SimpleModule {
        public StudentFormModule() : base(typeof(StudentForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<StudentForm>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel)
                ;
            block.ExportModuleScope<StudentFormModel>();
        }
    }

    public class StudentFormBase : View<StudentFormToken, StudentFormModel> {
    }

    /// <summary>
    /// Interaction logic for StudentForm.xaml
    /// </summary>
    public partial class StudentForm : StudentFormBase {
        public StudentForm() {
            InitializeComponent();
        }
    }
}
