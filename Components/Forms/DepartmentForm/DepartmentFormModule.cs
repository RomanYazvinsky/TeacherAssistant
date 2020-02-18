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
            registrationBlock.ExportModuleScope<DepartmentFormModel>();
            registrationBlock.ExportModuleScope<DepartmentForm>()
                .ImportProperty(form => form.ViewModel)
                .ImportProperty(form => form.ModuleToken);
        }
    }
}
