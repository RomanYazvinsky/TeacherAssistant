using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using Model.Models;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.State;

namespace TeacherAssistant.GroupTable {
    public class GroupTableModuleToken : PageModuleToken<GroupTableModule> {
        public GroupTableModuleToken(string title, GroupEntity group) :
            base(IdGenerator.GenerateId(), title) {
            this.Group = group;
        }

        public GroupEntity Group { get; }
    }

    public class GroupTableModule : Module {
        public GroupTableModule()
            : base(new[] {
                typeof(GroupTable),
                typeof(GroupTableModel),
            }) {
        }

        public override Control GetEntryComponent() {
            return this.Kernel?.Get<GroupTable>();
        }
    }

    /// <summary>
    /// Interaction logic for GroupTable.xaml
    /// </summary>
    public partial class GroupTable : View<GroupTableModel> {
        public GroupTable() {
            InitializeComponent();
            SortHelper.AddColumnSorting(Groups,
                new Dictionary<string, ListSortDirection> {{"Name", ListSortDirection.Ascending}});
        }
    }
}