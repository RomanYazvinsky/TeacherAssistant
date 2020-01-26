using System.Windows.Controls;
using Model.Models;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages.LessonForm {
    public class LessonFormModuleToken : PageModuleToken<LessonFormModule> {
        public LessonFormModuleToken(string title, LessonEntity lesson) :
            base(title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
    }

    public class LessonFormModule : Module {
        public LessonFormModule()
            : base(new[] {
                typeof(LessonForm),
                typeof(LessonFormModel),
            }) {
        }

        public override Control GetEntryComponent() {
            return this.Kernel?.Get<LessonForm>();
        }
    }

    /// <summary>
    /// Interaction logic for LessonForm.xaml
    /// </summary>
    public partial class LessonForm : View<LessonFormModel> {
        public LessonForm() {
            InitializeComponent();
        }
    }
}