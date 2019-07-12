using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace TeacherAssistant.ComponentsImpl.SchedulePage
{
    /// <summary>
    /// Interaction logic for SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : View<SchedulePageModel>
    {
        public SchedulePage(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
            SortHelper.AddColumnSorting(LessonList, new Dictionary<string, ListSortDirection> {
                {"Lesson.Date", ListSortDirection.Descending},
                {"Lesson.Schedule.Begin", ListSortDirection.Descending},
                {"Lesson.CreationDate", ListSortDirection.Descending},
            });
        }

        private void OnSelectItem(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (LessonList.SelectedItem == null)
            {
                return;
            }

            this.ViewModel.OpenRegistration();
        }
    }
}