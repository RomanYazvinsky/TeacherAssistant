using System.Collections.Generic;
using System.ComponentModel;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Pages.SchedulePage
{
    public class ScheduleToken : PageModuleToken<ScheduleModule>
    {
        public ScheduleToken(string title) : base(title)
        {
        }

        public override PageProperties PageProperties { get; } = new PageProperties
        {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class ScheduleModule : SimpleModule
    {
        public ScheduleModule() : base(typeof(SchedulePage))
        {
        }

        public override void Configure(IExportRegistrationBlock block)
        {
            block.DeclareComponent<SchedulePage>()
                .ImportProperty(page => page.ModuleToken)
                .ImportProperty(page => page.ViewModel);
            block.DeclareComponent<SchedulePageModel>();
        }
    }

    public class SchedulePageBase : View<ScheduleToken, SchedulePageModel>
    {
    }


    /// <summary>
    /// Interaction logic for SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : SchedulePageBase
    {
        public SchedulePage()
        {
            InitializeComponent();
            SortHelper.AddColumnSorting(LessonList, new Dictionary<string, ListSortDirection>
            {
                {"Lesson.Date", ListSortDirection.Descending},
                {"Lesson.Schedule.Begin", ListSortDirection.Descending},
                {"Lesson.CreationDate", ListSortDirection.Descending},
            });
        }
    }
}