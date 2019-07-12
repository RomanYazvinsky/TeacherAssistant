using System.Windows.Controls;
using TeacherAssistant.State;

namespace TeacherAssistant {
    public class ModalPageHost : MainWindowPageHost {
        public ModalPageHost(string providerId, PageService pageService ) : base(providerId, pageService) {


        }
    }
}