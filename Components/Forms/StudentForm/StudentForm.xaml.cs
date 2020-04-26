using Grace.DependencyInjection;
using JetBrains.Annotations;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.StudentForm {
    public class StudentFormToken : PageModuleToken<StudentFormModule> {
        public StudentFormToken(string title, [NotNull] StudentEntity student) :
            base(title) {
            this.Student = student;
        }

        [NotNull] public StudentEntity Student { get; }
        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 700,
            InitialWidth = 1000
        };
    }

    public class StudentFormModule : SimpleModule {
        public StudentFormModule() : base(typeof(StudentForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<StudentForm>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
            block.DeclareComponent<StudentFormModel>();
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
