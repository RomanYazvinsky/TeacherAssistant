using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DynamicData;
using Grace.DependencyInjection;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.State;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage
{
    public class TableLessonViewPageModel : AbstractModel<TableLessonViewPageModel>
    {
        private readonly LocalDbContext _db;
        private readonly IExportLocatorScope _scope;

        private readonly ObservableCollection<StudentLessonViewModel> _items =
            new ObservableCollection<StudentLessonViewModel>();

        public TableLessonViewPageModel(TableLessonViewToken token, LocalDbContext db, IExportLocatorScope scope)
        {
            _db = db;
            _scope = scope;
            this.Items = new CollectionViewSource
            {
                Source = _items
            };
            this.Columns.Add(BuildMissedLessonsColumn());
            this.WhenAnyValue(model => model.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ =>
                {
                    if (string.IsNullOrWhiteSpace(this.FilterText))
                    {
                        this.Items.View.Filter = __ => true;
                        return;
                    }

                    this.Items.View.Filter =
                        o => o is StudentLessonViewModel item &&
                             item.FullName.ToUpper().Contains(this.FilterText.ToUpper());
                });
            Init(token.Lesson);
        }

        private async void Init(LessonEntity lesson)
        {
            _items.Clear();
            this.Columns.Clear();
            _currentLesson = lesson;
            List<LessonEntity> lessonModels;
            lessonModels = lesson.Group == null
                ? await _db.Lessons.Include("StudentLessons").Where(model =>
                        model._StreamId == lesson._StreamId && (model._GroupId == null || model._GroupId == 0))
                    .ToListAsync()
                : await _db.Lessons
                    .Include("StudentLessons")
                    .Where(model => model._GroupId == lesson._GroupId
                                    || (model._StreamId == lesson._StreamId
                                        && !model._GroupId.HasValue))
                    .ToListAsync();
            lessonModels.Reverse();
            foreach (var lessonModel in lessonModels)
            {
                var lessonId = IdGenerator.GenerateId(10);
                this.Lessons.Add(lessonId, lessonModel);
                this.Columns.Add(BuildStudentLessonColumn(lessonId, lessonModel));
            }

            var studentModels = ((lesson.Group == null
                    ? lesson.Stream.Groups?.SelectMany(group => group.Students ?? new List<StudentEntity>())
                    : lesson.Group.Students?.ToList()) ?? new List<StudentEntity>())
                .Select(model => new StudentLessonViewModel(model, this.Lessons, _scope, _db))
                .OrderBy(view => view.FullName).ToList();
            _items.AddRange(studentModels);
        }

        private LessonEntity _currentLesson;

        private Dictionary<string, LessonEntity> Lessons { get; set; } = new Dictionary<string, LessonEntity>();

        [Reactive] public string FilterText { get; set; }

        public CollectionViewSource Items { get; set; }

        public ObservableCollection<DataGridColumn> Columns { get; set; } =
            new ObservableCollection<DataGridColumn>();

        private DataGridColumn BuildMissedLessonsColumn()
        {
            var dataGridTemplateColumn =
                new TextColumn
                {
                    Width = new DataGridLength(80),
                    Header = new TextBlock {Text = Localization["Пропуски"]},
                    Binding = new Binding("MissedLessons"),
                    IsReadOnly = true,
                    CanUserSort = false
                };
            return dataGridTemplateColumn;
        }

        private DataGridColumn BuildStudentLessonColumn(string id, LessonEntity entity)
        {
            var dataGridTemplateColumn =
                new TextColumn
                {
                    Width = new DataGridLength(90),
                    MinWidth = 50,
                    Header = new TextBlock
                    {
                        Text = Localization["common.lesson.type." + entity.LessonType] + "\n " +
                               entity.Date?.ToString("dd.MM"),
                        Foreground = entity.Id == _currentLesson.Id ? Brushes.Red : Brushes.Black
                    },
                    Binding = new Binding {Path = new PropertyPath($"LessonToLessonMark[{id}].Mark")},
                    CanUserSort = false
                };
            var cellStyle = new Style();
            var backGround = new Setter(Control.BackgroundProperty, new Binding($"LessonToLessonMark[{id}].Color"));
            var menu = new Setter(FrameworkElement.ContextMenuProperty, BuildLessonCellContextMenu(id));
            cellStyle.Setters.Add(backGround);
            cellStyle.Setters.Add(menu);
            dataGridTemplateColumn.CellStyle = cellStyle;
            return dataGridTemplateColumn;
        }

        private ContextMenu BuildLessonCellContextMenu(string id) {
            var menu = new ContextMenu();
            var menuItem = new MenuItem();
            menuItem.SetBinding(MenuItem.CommandProperty,
                new Binding($"LessonToLessonMark[{id}].ToggleRegistrationHandler"));
            menuItem.Header = "Отметить/пропуск";
            menu.Items.Add(menuItem);
            return menu;
        }

        protected override string GetLocalizationKey()
        {
            return "common.lesson.view";
        }
    }
}
