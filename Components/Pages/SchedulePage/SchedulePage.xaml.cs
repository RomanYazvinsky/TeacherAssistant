﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Grace.DependencyInjection;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.ComponentsImpl.SchedulePage {
    public class ScheduleToken : PageModuleToken<ScheduleModule> {
        public ScheduleToken(string title) : base(title) {
        }
    }

    public class ScheduleModule : SimpleModule {
        public ScheduleModule() : base(typeof(SchedulePage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<SchedulePage>(this.ModuleToken.Id)
                .ImportProperty(page => page.ModuleToken)
                .ImportProperty(page => page.ViewModel)
                ;
            block.ExportModuleScope<SchedulePageModel>(this.ModuleToken.Id);
        }
    }

    public class SchedulePageBase : View<ScheduleToken, SchedulePageModel> {
    }


    /// <summary>
    /// Interaction logic for SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : SchedulePageBase {
        public SchedulePage() {
            InitializeComponent();
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