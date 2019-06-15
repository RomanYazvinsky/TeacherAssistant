using System.Windows.Controls;
using System.Windows.Input;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;

namespace TeacherAssistant.SchedulePage
{
    /// <summary>
    /// Interaction logic for SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : UserControl
    {
        public SchedulePage(string id)
        {
            InitializeComponent();
            var schedulePageModel = new SchedulePageModel();
            DataContext = schedulePageModel;
            schedulePageModel.Init(id);

            SortHelper.AddColumnSorting(LessonList);
        }

        private void OnSelectItem(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (LessonList.SelectedItem == null)
            {
                return;
            }

            ((SchedulePageModel)DataContext).SelectedLesson = (LessonModel)LessonList.SelectedItem;
        }
    }
}
