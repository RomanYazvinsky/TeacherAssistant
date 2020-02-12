﻿using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.StudentViewPage {
    public class StudentViewPageToken : PageModuleToken<StudentViewPageModule> {
        public StudentViewPageToken(string title, StudentEntity student) : base(title) {
            this.Student = student;
        }

        public StudentEntity Student { get; }
    }

    public class StudentViewPageModule : SimpleModule {
        public StudentViewPageModule() : base(typeof(StudentViewPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<StudentViewPageModel>();
            block.ExportModuleScope<StudentViewPage>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class StudentViewPageBase : View<StudentViewPageToken, StudentViewPageModel> {
    }

    /// <summary>
    /// Interaction logic for StudentViewPage.xaml
    /// </summary>
    public partial class StudentViewPage : StudentViewPageBase {
        public StudentViewPage() {
            InitializeComponent();
        }
    }
}
