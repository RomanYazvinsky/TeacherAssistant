using System.Windows.Controls;
using Model.Models;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.State;

namespace TeacherAssistant.Forms.GroupForm {
    public class GroupFormModuleToken : PageModuleToken<GroupFormModule> {
        public GroupFormModuleToken(string title, GroupEntity group) : base(IdGenerator.GenerateId(), title) {
            this.Group = group;
        }

        public GroupEntity Group { get; }
    }

    public class GroupFormModule : Module {
        public GroupFormModule()
            : base(new[] {
                typeof(GroupForm),
                typeof(GroupFormModel),
            }) {
        }


        public override Control GetEntryComponent() {
            return this.Kernel?.Get<GroupForm>();
        }
    }

    /// <summary>
    /// Interaction logic for GroupForm.xaml
    /// </summary>
    public partial class GroupForm : View<GroupFormModel> {
        public GroupForm() {
            InitializeComponent();
        }
    }
}