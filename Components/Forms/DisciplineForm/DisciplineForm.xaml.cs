using JetBrains.Annotations;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Forms.DisciplineForm
{
    public class DisciplineFormToken : PageModuleToken<DisciplineFormModule>
    {
        public DisciplineFormToken(string title, DisciplineEntity discipline = null) : base(title)
        {
            this.Discipline = discipline ?? new DisciplineEntity();
        }
        [NotNull] public DisciplineEntity Discipline { get; }

        public override PageProperties PageProperties { get; } = new PageProperties
        {
            InitialHeight = 200,
            InitialWidth = 400
        };
    }

    public class DisciplineFormBase : View<DisciplineFormToken, DisciplineFormModel>
    {
    }

    public partial class DisciplineForm : DisciplineFormBase
    {
        public DisciplineForm()
        {
            InitializeComponent();
        }
    }
}
