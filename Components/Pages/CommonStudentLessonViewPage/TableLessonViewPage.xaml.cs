using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Binding;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TableLessonViewToken : PageModuleToken<TableLessonViewModule> {
        public TableLessonViewToken(string title, LessonEntity lesson) :
            base(title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class TableLessonViewModule : SimpleModule {
        public TableLessonViewModule(): base(typeof(TableLessonViewPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportInitialize<IInitializable>(initializable => initializable.Initialize());
            block.ExportModuleScope<TableLessonViewPageModel>();
            block.ExportModuleScope<TableLessonViewPage>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class TableLessonViewPageBase : View<TableLessonViewToken, TableLessonViewPageModel> {
    }

    /// <summary>
    /// Interaction logic for CommonStudentLessonViewPage.xaml
    /// </summary>
    public partial class TableLessonViewPage : TableLessonViewPageBase, IInitializable {
        public TableLessonViewPage() {
            InitializeComponent();
        }

        public void Initialize() {
            var columns = this.ViewModel.Columns;
            var staticColumns = new List<DataGridColumn>(Table.Columns);
            var fromEventPattern = columns.ToObservableChangeSet();

            var observable = fromEventPattern.Throttle(TimeSpan.FromMilliseconds(300));
            fromEventPattern
                .StartWith(new List<IChangeSet<DataGridColumn>>())
                .Buffer(observable)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ => {
                    var dynamicColumns = Table.Columns.Where(item => !staticColumns.Contains(item)).ToList();
                    foreach (var item in dynamicColumns) {
                        Table.Columns.Remove(item);
                    }

                    foreach (var dataGridColumn in columns) {
                        Table.Columns.Add(dataGridColumn);
                    }
                });
        }
    }
}
