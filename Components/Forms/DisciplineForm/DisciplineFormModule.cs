using Grace.DependencyInjection;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Forms.DisciplineForm
{
    public class DisciplineFormModule: SimpleModule
    {
        public DisciplineFormModule() : base(typeof(DisciplineForm))
        {
        }

        public override void Configure(IExportRegistrationBlock registrationBlock)
        {
            registrationBlock.DeclareComponent<DisciplineFormModel>();
            registrationBlock.DeclareComponent<DisciplineForm>()
                .ImportProperty(form => form.ViewModel)
                .ImportProperty(form => form.ModuleToken);
        }
    }
}
