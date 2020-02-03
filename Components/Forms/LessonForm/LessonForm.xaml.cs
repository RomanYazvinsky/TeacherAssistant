using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages.LessonForm {
    public class LessonFormToken : PageModuleToken<LessonFormModule> {
        public LessonFormToken(string title, LessonEntity lesson) :
            base(title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
    }

    public class LessonFormModule : SimpleModule {
        public LessonFormModule(): base(typeof(LessonForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<LessonForm>(this.ModuleToken.Id)
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
            block.ExportModuleScope<LessonFormModel>(this.ModuleToken.Id);
        }
    }

    public class LessonFormBase : View<LessonFormToken, LessonFormModel> {
    }

    /// <summary>
    /// Interaction logic for LessonForm.xaml
    /// </summary>
    public partial class LessonForm : LessonFormBase {
        public LessonForm() {
            InitializeComponent();
        }
    }
}