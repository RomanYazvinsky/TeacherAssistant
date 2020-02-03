using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Forms.NoteForm {
    public class NoteFormToken : PageModuleToken<NoteFormModule> {
        public NoteEntity Entity { get; }

        public NoteFormToken(string title, NoteEntity entity) : base(title) {
            this.Entity = entity;
        }
    }

    public class NoteFormModule : SimpleModule {
        public NoteFormModule() : base(typeof(NoteForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<NoteForm>(this.ModuleToken.Id)
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel)
                ;
            block.ExportModuleScope<NoteFormModel>(this.ModuleToken.Id);
        }
    }

    public class NoteFormBase : View<NoteFormToken, NoteFormModel> {
    }

    /// <summary>
    /// Interaction logic for LessonForm.xaml
    /// </summary>
    public partial class NoteForm : NoteFormBase {
        public NoteForm(string id) {
            InitializeComponent();
        }
    }
}