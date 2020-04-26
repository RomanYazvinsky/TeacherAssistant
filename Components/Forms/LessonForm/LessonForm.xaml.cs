using Grace.DependencyInjection;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.LessonForm {
    public class LessonFormToken : PageModuleToken<LessonFormModule> {
        public LessonFormToken(string title, LessonEntity lesson, IComponentHost componentHost) :
            base(title)
        {
            this.Lesson = lesson;
            this.ComponentHost = componentHost;
        }

        public LessonEntity Lesson { get; }

        public IComponentHost ComponentHost { get; }
        public override PageProperties PageProperties { get; }= new PageProperties {
            InitialHeight = 550,
            InitialWidth = 600
        };
    }

    public class LessonFormModule : SimpleModule {
        public LessonFormModule(): base(typeof(LessonForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<LessonForm>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
            block.DeclareComponent<LessonFormModel>();
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
