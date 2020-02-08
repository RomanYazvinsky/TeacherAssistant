using System;
using System.Collections.Generic;
using Grace.DependencyInjection;
using JetBrains.Annotations;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Forms.NoteForm {
    public class NoteListFormToken : PageModuleToken<NoteListFormModule> {
        [NotNull] public IEnumerable<NoteEntity> Notes { get; }

        [NotNull] public Func<NoteEntity> NoteFactory { get; }
        [CanBeNull] public NoteEntity SelectedNote { get; }

        public NoteListFormToken(
            string title,
            Func<NoteEntity> noteFactory,
            IEnumerable<NoteEntity> notes = null,
            NoteEntity selectedNote = null
            ) : base(title)
        {
            this.Notes = notes ?? new List<NoteEntity>();
            NoteFactory = noteFactory;
            SelectedNote = selectedNote;
        }
    }

    public class NoteListFormModule : SimpleModule {
        public NoteListFormModule() : base(typeof(NoteListListForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<NoteListListForm>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
            block.ExportModuleScope<NoteFormModel>();
        }
    }

    public class NoteListFormBase : View<NoteListFormToken, NoteFormModel> {
    }

    /// <summary>
    /// Interaction logic for LessonForm.xaml
    /// </summary>
    public partial class NoteListListForm : NoteListFormBase {
        public NoteListListForm() {
            InitializeComponent();
        }
    }
}
