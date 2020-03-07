using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TeacherAssistant.Models;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns {
    public class StudentLessonColumn : DataGridTextColumn {
        private readonly StudentLessonColumnHelper _helper;
        private readonly LessonEntity _lesson;
        private readonly bool _async;

        public StudentLessonColumn(StudentLessonColumnHelper helper, LessonEntity lesson, bool async = false) {
            _helper = helper;
            _lesson = lesson;
            _async = async;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem) {
            var textBlock = new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center
            };
            this.Dispatcher?.BeginInvoke(
                DispatcherPriority.Background,
                new Action<TextBlock>(x => {
                    x.SetValue(FrameworkElement.StyleProperty, this.ElementStyle);
                }),
                textBlock);

            cell.DataContextChanged += _async
                ? _helper.CreateAsyncHandler(textBlock, _lesson.Id)
                : _helper.CreateEagerHandler(textBlock, _lesson.Id);
            cell.DataContext = dataItem;
            _helper.UpdateCellValue(textBlock, (StudentRowViewModel) dataItem, _lesson.Id);
            return textBlock;
        }

       
    }
}