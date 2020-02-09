using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.StreamTable {
    public class StreamTableToken : PageModuleToken<StreamTableModule> {
        public StreamTableToken(string title) :
            base(title) {
        }

    }

    public class StreamTableModule : SimpleModule {
        public StreamTableModule()
            : base(typeof(StreamTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<StreamTableModel>();
            block.ExportModuleScope<StreamTable>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class StreamTableBase : View<StreamTableToken, StreamTableModel> {
    }

    /// <summary>
    /// Interaction logic for StreamTable.xaml
    /// </summary>
    public partial class StreamTable : StreamTableBase {
        public StreamTable() {
            InitializeComponent();
            SortHelper.AddColumnSorting(Streams,
                new Dictionary<string, ListSortDirection> {{"Name", ListSortDirection.Ascending}});
        }
    }
}
