using Grace.DependencyInjection;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Forms.DepartmentForm
{
    public class DepartmentFormModule: SimpleModule
    {
        public DepartmentFormModule() : base(typeof(DepartmentForm))
        {
        }

        public override void Configure(IExportRegistrationBlock registrationBlock)
        {
            registrationBlock.DeclareComponent<DepartmentFormModel>();
            registrationBlock.DeclareComponent<DepartmentForm>()
                .ImportProperty(form => form.ViewModel)
                .ImportProperty(form => form.ModuleToken);
        }
    }
}
