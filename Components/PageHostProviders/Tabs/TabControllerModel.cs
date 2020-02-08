using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.Pages;
using TeacherAssistant.Utils;

namespace TeacherAssistant {
    public class TabControllerModel : AbstractModel<TabControllerModel> {
        private static readonly string LocalizationKey = "tabs";

        static TabControllerModel() {
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
                        "Пропуски: {0} -> Л {1} | П {2} | Лб {3}"
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

        public TabControllerModel(
            MainReducer reducer,
            PageControllerReducer controllerReducer,
            TabPageHost host) {
            reducer.Select(state => state.DragData).Subscribe(data => {
                if (data == null) {
                    foreach (var tabItem in this.Tabs) {
                        tabItem.MouseMove -= OpenTabOnHover;
                    }
                }
                else {
                    foreach (var tabItem in this.Tabs) {
                        tabItem.MouseMove += OpenTabOnHover;
                    }
                }
            });
            reducer.Select(state => state.FullscreenMode)
                .Subscribe(isFullscreen => this.IsHeaderVisible = !isFullscreen);
            host.WhenTabAdded
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(tab => {
                this.Tabs.Add(tab);
                this.ActiveTab = tab;
                this.ActiveTab.AllowDrop = true;
            });
            host.WhenTabClosed
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(RemoveTab);
            this.WhenAnyValue(model => model.ActiveTab)
                .Where(LambdaHelper.NotNull)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(tab => {
                    tab.IsSelected = true;
                    var activeTabUid = tab.Uid;
                    controllerReducer.DispatchSetValueAction(state => state.SelectedPage, host.Pages[activeTabUid].Token);
                });
        }

        private void OpenTabOnHover(object sender, MouseEventArgs parameters) {
            this.ActiveTab = (TabItem) sender;
        }

        private void RemoveTab(TabItem control) {
            control.Template = null;
            var tabToRemove = this.Tabs.FirstOrDefault(tabItem =>
                tabItem.Uid.Equals(control.Uid));
            if (tabToRemove != null) {
                this.Tabs.Remove(tabToRemove);
            }
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        [Reactive] public bool IsHeaderVisible { get; set; } = true;

        [NotNull]
        public ObservableCollection<TabItem> Tabs { get; set; } =
            new ObservableCollection<TabItem>();

        [Reactive] [CanBeNull] public TabItem ActiveTab { get; set; }


        /*
        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source) {
            var windowHostId = this._pageService.GetProviderId(this.Id);
            var newTabProviderId = this._pageService.OpenPage(windowHostId, new PageProperties<PageController>());
            var pageContainerProvider = this._pageService.GetProvider<Window>(windowHostId);
            var window = pageContainerProvider.GetCurrentControl(newTabProviderId);
            var activeTabId = this.ActiveTab.Uid;
            this._pageService.MovePage<TabItem>(newTabProviderId, activeTabId);
            var child = window.GetChildOfType<TabablzControl>();
            return new NewTabHost<Window>(window, child);
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window) {
            this._pageService.ClosePage(window.Uid);
            this._pageService.RemovePageHost(window.Uid);
            return TabEmptiedResponse.DoNothing;
        }*/

        public override void Dispose() {
            base.Dispose();
        }
    }
}
