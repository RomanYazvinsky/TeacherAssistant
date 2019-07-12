using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DynamicData;
using ReactiveUI;
using Splat;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.CellTemplates;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    /// <summary>
    /// Interaction logic for CommonStudentLessonViewPage.xaml
    /// </summary>
    public partial class CommonStudentLessonViewPage : View<CommonStudentLessonViewPageModel> {
        public CommonStudentLessonViewPage(string id) {
            InitializeComponent();
            InitializeViewModel(id);
//            ViewModelLoaded += (sender, model) => {
                var dynamicColumns = new List<DataGridColumn>(this.ViewModel.Columns);
                Table.Columns.AddRange(dynamicColumns);
                var fromEventPattern = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => this.ViewModel.Columns.CollectionChanged += h,
                    h => this.ViewModel.Columns.CollectionChanged -= h);
                var observable = fromEventPattern.Throttle(TimeSpan.FromMilliseconds(500));
                fromEventPattern.Buffer(observable).Subscribe((h) => {
                    Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                        dynamicColumns.ForEach(item => Table.Columns.Remove(item));
                        dynamicColumns.Clear();
                        dynamicColumns.AddRange(this.ViewModel.Columns);
                        Table.Columns.AddRange(this.ViewModel.Columns);
                    }));
                });
//            };
        }
    }
}