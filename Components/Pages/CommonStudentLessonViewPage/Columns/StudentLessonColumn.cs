using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using FontAwesome5;
using TeacherAssistant.Models;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns {
    public sealed class StudentLessonColumn : DataGridTextColumn {
        private readonly StudentLessonColumnHelper _helper;
        private readonly LessonEntity _lesson;
        private readonly bool _async;

        public StudentLessonColumn(StudentLessonColumnHelper helper, LessonEntity lesson, bool async = false) {
            _helper = helper;
            _lesson = lesson;
            _async = async;

            this.Binding = new Binding(
                $"{nameof(StudentRowViewModel.LessonToLessonMark)}[{lesson.Id}].{nameof(StudentLessonCellViewModel.Mark)}"
            );
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem) {
            var grid = new Grid {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition {
                Width = new GridLength(1, GridUnitType.Auto)
            });
            var notesIcon = new FontAwesome {
                Icon = EFontAwesomeIcon.Solid_InfoCircle
            };
            var textBlock = new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center
            };
            grid.SetBinding(Panel.BackgroundProperty, new Binding {
                RelativeSource = new RelativeSource {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorLevel = 1,
                    AncestorType = typeof(DataGridCell)
                },
                Path = new PropertyPath(CellStyleExtensions.ContentBackgroundProperty)
            });
            this.Dispatcher?.BeginInvoke(
                DispatcherPriority.Background,
                new Action<TextBlock>(x => {
                    x.SetValue(FrameworkElement.StyleProperty, this.ElementStyle);
                    grid.Children.Add(textBlock);
                    grid.Children.Add(notesIcon);
                    Grid.SetRow(textBlock, 0);
                    Grid.SetRow(notesIcon, 0);
                    Grid.SetColumn(textBlock, 0);
                    Grid.SetColumn(notesIcon, 1);
                }),
                textBlock);
            cell.DataContextChanged += _async
                ? _helper.CreateAsyncHandler(grid, textBlock, notesIcon, _lesson.Id)
                : _helper.CreateEagerHandler(grid, textBlock, notesIcon, _lesson.Id);
            cell.DataContext = dataItem;
            _helper.UpdateCellValue(cell, grid, textBlock, notesIcon, (StudentRowViewModel) dataItem, _lesson.Id);
            return grid;
        }
    }
}