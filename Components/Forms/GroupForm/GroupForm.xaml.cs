using System.Reactive.Disposables;
using Grace.DependencyInjection;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.State;

namespace TeacherAssistant.Forms.GroupForm {
    public class GroupFormToken : PageModuleToken<GroupFormModule> {
        public GroupFormToken(string title, GroupEntity group) : base(title) {
            this.Group = group;
        }

        public GroupEntity Group { get; }
    }

    public class GroupFormModule : SimpleModule {
        public GroupFormModule(): base(typeof(GroupForm)) {
        }

        public override void Configure(IExportRegistrationBlock registrationBlock) {
            registrationBlock.ExportInitialize<IInitializable>(initializable => initializable.Initialize());
            registrationBlock.ExportModuleScope<GroupForm>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
            registrationBlock.ExportModuleScope<GroupFormModel>();
        }
    }

    public class GroupFormBase : View<GroupFormToken, GroupFormModel> {
    }

    /// <summary>
    /// Interaction logic for GroupForm.xaml
    /// </summary>
    public partial class GroupForm : GroupFormBase, IInitializable {
        public GroupForm() {
            InitializeComponent();
        }

        public void Initialize() {
            // this.WhenActivated(c => {
            //     this.BindValidation(this.ViewModel, model => model.GroupName, form => form.NameBox.Text).DisposeWith(c);
            //     
            // });
        }
    }
}
