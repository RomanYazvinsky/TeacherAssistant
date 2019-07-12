using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Containers;
using Dragablz;
using FontAwesome5;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Pages;
using TeacherAssistant.State;

namespace TeacherAssistant {
    public class TabManagerModel : AbstractModel, IInterTabClient {
        private static readonly string LocalizationKey = "tabs";
        private TabPageHost _tabPageHost;

        static TabManagerModel() {
            Localization.AddLanguageResources
            (
                CultureInfo.GetCultureInfo("ru-RU"),
                new Dictionary<string, string> {
                    {
                        "page.student.view.external.lessons",
                        "Занятия с другими группами ({0})"
                    },
                    {"page.student.view.student.notes", "Заметки по студенту ({0})"},
                    {"page.student.view.lesson.notes", "Заметки по занятиям ({0})"},
                    {"page.student.view.student.notes.date", "Дата"},
                    {"page.student.view.student.notes.description", "Заметка"},
                    {"page.student.view.stream.course", "Курс - {0}"},
                    {"page.student.view.missed.lessons.empty", "Пропусков нет"},
                    {"page.student.view.average.mark", "Средняя оценка"},
                    {"page.student.view.marks", "Оценки"},
                    {"page.student.view.mark.label", "Символы"},
                    {"page.student.view.exam.answer.label", "Ответ"},
                    {"page.student.view.session.result.label", "Итог"},
                    {"page.student.view.attestation.header.label", "А{0}"},
                    {"page.student.view.attestation.avg.label", "АC"},
                    {"page.student.view.missed.lessons", "Пропуски: {0} ({1}/{2}/{3})"}, {
                        "page.student.view.discipline.lessons",
                        "Кол-во по плану: лекций {0} / практ. {1} / лаб. {2}"
                    }, {
                        "page.registration.active.student.info",
                        "{0} {1} {2} \nПропуски: {3} -> Л {4} | П {5} | Лб {6}"
                    },
                    {$"common.lesson.type.{LessonType.Unknown}", "#########"},
                    {$"common.lesson.type.{LessonType.Lecture}", "Лекция"},
                    {$"common.lesson.type.{LessonType.Practice}", "Практика"},
                    {$"common.lesson.type.{LessonType.Laboratory}", "Лабораторная"},
                    {$"common.lesson.type.{LessonType.Attestation}", "Аттестация"},
                    {$"common.lesson.type.{LessonType.Exam}", "Экзамен"},
                    {"common.empty.dropdown", "(Пусто)"},
                    {"student.form.lastname.label", "Фамилия"},
                    {"student.form.name.label", "Имя"},
                    {"student.form.secondname.label", "Отчество"},
                    {"student.form.phone.label", "Телефон"},
                    {"student.form.card.id.label", "Card ID"},
                    {"student.form.card.uid.label", "Card UID"},
                    {"student.form.email.label", "Email"},
                }
            );   
        }
        public TabManagerModel(string id) : base(id) {
            _tabPageHost = new TabPageHost(id, this.PageService);
            _tabPageHost.PageAdded += (sender, control) => {
                this.Tabs.Add(control);
                this.ActiveTab = control;
                this.ActiveTab.AllowDrop = true;
                this.ActiveTab.MouseMove += (o, args) => {
                    var dragData = StoreManager.Get<DragData>("DragData");
                    if (dragData != null) {
                        this.ActiveTab = control;
                    }
                };
            };
            _tabPageHost.PageAttached += (sender, control) => { this.ActiveTab = control; };
            _tabPageHost.PageClosed += RemoveTab;
            this.WhenAnyValue(model => model.ActiveTab)
                .Where(NotNull)
                .Subscribe
                (
                    tab => {
                        UpdateFromAsync(() => tab.IsSelected = true);
                        StoreManager.Publish(StoreManager.Get<List<ButtonConfig>>(tab.Uid + ".Controls"), this.Id,
                            "Controls");
                    }
                );
            Select<bool>("FullscreenMode")
                .Subscribe
                (
                    isFullscreen => this.IsHeaderVisible = !isFullscreen
                );
            this.CloseCallback = control => {
                this.PageService.ClosePage(((Control) control.DragablzItem.Content).Uid);
            };
            this.PageService.RegisterPageHost(_tabPageHost);
        }

        
        private void RemoveTab(object sender, Control control) {
            ((TabItem) control).Template = null;
            var tabToRemove = this.Tabs.FirstOrDefault(tabItem =>
                tabItem.Uid.Equals(control.Uid));
            if (tabToRemove != null) {
                this.Tabs.Remove(tabToRemove);
            }
        }

        public override List<ButtonConfig> GetControls() {
            return new List<ButtonConfig>();
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        [Reactive] public bool IsHeaderVisible { get; set; }
        
        [Reactive] public ItemActionCallback CloseCallback { get; set; }

        public ObservableRangeCollection<TabItem> Tabs { get; set; } =
            new WpfObservableRangeCollection<TabItem>();

        [Reactive] public TabItem ActiveTab { get; set; }

        public override Task Init() {
            return Task.CompletedTask;
        }

        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source) {
            var windowHostId = this.PageService.GetProviderId(this.Id);
            var newTabProviderId = this.PageService.OpenPage
                (windowHostId, new PageProperties {PageType = typeof(MainWindowPage)});
            var pageContainerProvider = this.PageService.GetProvider<Window>(windowHostId);
            var window = pageContainerProvider.GetCurrentControl(newTabProviderId);
            var activeTabId = this.ActiveTab.Uid;
            this.PageService.MovePage<TabItem>(newTabProviderId, activeTabId);
            var child = window.GetChildOfType<TabablzControl>();
            return new NewTabHost<Window>(window, child);
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window) {
            this.PageService.ClosePage(window.Uid);
            this.PageService.RemovePageHost(window.Uid);
            return TabEmptiedResponse.DoNothing;
        }
    }
}