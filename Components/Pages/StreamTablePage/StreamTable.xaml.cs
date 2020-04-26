using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.StreamTable {
    public class StreamTableToken : PageModuleToken<StreamTableModule> {
        public StreamTableToken(string title) :
            base(title) {
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class StreamTableModule : SimpleModule {
        public StreamTableModule()
            : base(typeof(StreamTable)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<StreamTableModel>();
            block.DeclareComponent<StreamTable>()
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
