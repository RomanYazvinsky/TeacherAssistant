using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Forms.StreamForm {
    public class StreamFormToken : PageModuleToken<StreamFromModule> {
        public StreamFormToken(string title, StreamEntity stream) : base(title) {
            this.Stream = stream;
        }

        public StreamEntity Stream { get; }
    }

    public class StreamFromModule : SimpleModule {
        public StreamFromModule() : base(typeof(StreamForm)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<StreamForm>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel)
                ;
            block.ExportModuleScope<StreamFormModel>();
        }
    }

    public class StreamFormBase : View<StreamFormToken, StreamFormModel> {
    }

    /// <summary>
    /// Interaction logic for StreamForm.xaml
    /// </summary>
    public partial class StreamForm : StreamFormBase {
        public StreamForm() {
            InitializeComponent();
        }
    }
}
