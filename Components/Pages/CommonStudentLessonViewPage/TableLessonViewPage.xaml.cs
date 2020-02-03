using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using DynamicData;
using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TableLessonViewToken : PageModuleToken<TableLessonViewModule> {
        public TableLessonViewToken(string title, LessonEntity lesson) :
            base(title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
    }

    public class TableLessonViewModule : SimpleModule {
        public TableLessonViewModule(): base(typeof(TableLessonViewPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportInitialize<IInitializable>(initializable => initializable.Initialize());
            block.ExportModuleScope<TableLessonViewPageModel>(this.ModuleToken.Id);
            block.ExportModuleScope<TableLessonViewPage>(this.ModuleToken.Id)
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel)
                ;
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
            var observableRangeCollection = this.ViewModel.Columns;
            var dynamicColumns = new List<DataGridColumn>(observableRangeCollection);
            Table.Columns.AddRange(dynamicColumns);
            var fromEventPattern = observableRangeCollection.Changes();

            var observable = fromEventPattern.Throttle(TimeSpan.FromMilliseconds(500));
            fromEventPattern
                .Buffer(observable)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe((h) => {
                    foreach (var item in dynamicColumns) {
                        Table.Columns.Remove(item);
                    }

                    dynamicColumns.Clear();
                    foreach (var dataGridColumn in observableRangeCollection) {
                        dynamicColumns.Add(dataGridColumn);
                        Table.Columns.Add(dataGridColumn);
                    }
                });
        }
    }
}