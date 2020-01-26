using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using DynamicData;
using Model.Models;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.State;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TableLessonViewModuleToken : PageModuleToken<TableLessonViewModule> {
        public TableLessonViewModuleToken(string title, LessonEntity lesson) :
            base(IdGenerator.GenerateId(), title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
    }

    public class TableLessonViewModule : Module {
        public TableLessonViewModule()
            : base(new[] {
                typeof(TableLessonViewPage),
                typeof(TableLessonViewPageModel),
            }) {
        }

        public override Control GetEntryComponent() {
            return this.Kernel?.Get<TableLessonViewPage>();
        }
    }

    /// <summary>
    /// Interaction logic for CommonStudentLessonViewPage.xaml
    /// </summary>
    public partial class TableLessonViewPage : View<TableLessonViewPageModel> {
        public TableLessonViewPage() {
            InitializeComponent();
        }

        public override void Initialize() {
            var dynamicColumns = new List<DataGridColumn>(this.ViewModel.Columns);
            Table.Columns.AddRange(dynamicColumns);
            var fromEventPattern = this.ViewModel.Columns.Changes();

            var observable = fromEventPattern.Throttle(TimeSpan.FromMilliseconds(500));
            fromEventPattern
                .Buffer(observable)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe((h) => {
                    dynamicColumns.ForEach(item => Table.Columns.Remove(item));
                    dynamicColumns.Clear();
                    dynamicColumns.AddRange(this.ViewModel.Columns);
                    Table.Columns.AddRange(this.ViewModel.Columns);
                });
        }
    }
}