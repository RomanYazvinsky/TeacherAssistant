using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using Ninject;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.ComponentsImpl.SchedulePage {
    public class ScheduleToken : PageModuleToken<ScheduleModule> {
        public ScheduleToken(string title) : base(title) {
        }
    }

    public class ScheduleModule : Module {
        public ScheduleModule()
            : base(new[] {
                typeof(SchedulePage),
                typeof(SchedulePageModel)
            }) {
        }


        public override Control GetEntryComponent() {
            return this.Kernel?.Get<SchedulePage>();
        }
    }

    /// <summary>
    /// Interaction logic for SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : View<SchedulePageModel> {
        public SchedulePage() {
            InitializeComponent();
            // SetViewModel(id);
            SortHelper.AddColumnSorting(LessonList, new Dictionary<string, ListSortDirection> {
                {"Lesson.Date", ListSortDirection.Descending},
                {"Lesson.Schedule.Begin", ListSortDirection.Descending},
                {"Lesson.CreationDate", ListSortDirection.Descending},
            });
        }

        private void OnSelectItem(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            if (LessonList.SelectedItem == null) {
                return;
            }

            this.ViewModel.OpenRegistration();
        }
    }
}