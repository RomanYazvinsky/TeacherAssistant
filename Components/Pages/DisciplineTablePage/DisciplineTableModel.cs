using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Containers;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.DisciplineForm;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.PageHostProviders;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.DisciplineTablePage {
    public class DisciplineTableModel : AbstractModel<DisciplineTableModel> {
        private readonly LocalDbContext _db;
        private readonly IPageHost _host;
        private static readonly string LocalizationKey = "page.discipline.table";

        public DisciplineTableModel(LocalDbContext db, WindowPageHost host) {
            _db = db;
            _host = host;
            this.DeleteMenuButtonConfig = new ButtonConfig {
                Command = ReactiveCommand.Create(DeleteDiscipline)
            };
            this.ShowMenuButtonConfig = new ButtonConfig {
                Command = ReactiveCommand.Create(ShowDiscipline)
            };
            Observable.Merge(WhenAdded<DisciplineEntity>(), WhenRemoved<DisciplineEntity>(), WhenUpdated<DisciplineEntity>())
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ =>
                {
                    this.Disciplines.Clear();
                    this.Disciplines.AddRange(db.Disciplines.ToList());
                });
            this.Disciplines.Clear();
            this.Disciplines.AddRange(db.Disciplines.ToList());
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void ShowDiscipline() {
            _host.AddPageAsync<DisciplineFormModule, DisciplineFormToken>(
                    new DisciplineFormToken(this.SelectedDiscipline.Name, this.SelectedDiscipline));
        }

        private void DeleteDiscipline() {
            if (this.SelectedDiscipline == null) {
                return;
            }
            var messageBoxResult = MessageBox.Show(Localization["Вы уверены, что хотите удалить дисциплину?"],
                Localization["Подтверждение удаления"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult != MessageBoxResult.Yes)
            {
                return;
            }
            _db.Disciplines.Remove(this.SelectedDiscipline);
            _db.SaveChangesAsync();
        }

        public ObservableCollection<DisciplineEntity> Disciplines { get; set; } =
            new ObservableCollection<DisciplineEntity>();

        [Reactive] public DisciplineEntity SelectedDiscipline { get; set; }

        public ButtonConfig DeleteMenuButtonConfig { get; set; }
        public ButtonConfig ShowMenuButtonConfig { get; set; }
    }
}
