using JetBrains.Annotations;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Forms.DepartmentForm
{
    public class DepartmentFormToken : PageModuleToken<DepartmentFormModule>
    {
        public DepartmentFormToken(string title, DepartmentEntity discipline = null) : base(title)
        {
            this.Department = discipline ?? new DepartmentEntity();
        }
        [NotNull] public DepartmentEntity Department { get; }

        public override PageProperties PageProperties { get; } = new PageProperties
        {
            InitialHeight = 200,
            InitialWidth = 400
        };
    }

    public class DepartmentFormBase : View<DepartmentFormToken, DepartmentFormModel>
    {
    }

    public partial class DepartmentForm : DepartmentFormBase
    {
        public DepartmentForm()
        {
            InitializeComponent();
        }
    }
}
