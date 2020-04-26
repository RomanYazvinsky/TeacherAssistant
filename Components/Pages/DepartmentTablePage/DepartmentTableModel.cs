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
using TeacherAssistant.Database;
using TeacherAssistant.Forms.DepartmentForm;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.DepartmentTablePage {
    public class DepartmentTableModel : AbstractModel<DepartmentTableModel> {
        private readonly LocalDbContext _db;
        private readonly IComponentHost _host;
        private static readonly string LocalizationKey = "page.department.table";

        public DepartmentTableModel(LocalDbContext db, WindowComponentHost host) {
            _db = db;
            _host = host;
            this.DeleteMenuButtonConfig = new ButtonConfig {
                Command = ReactiveCommand.Create(DeleteDepartment)
            };
            this.ShowMenuButtonConfig = new ButtonConfig {
                Command = ReactiveCommand.Create(ShowDepartment)
            };
            Observable.Merge(WhenAdded<DepartmentEntity>(), WhenRemoved<DepartmentEntity>(), WhenUpdated<DepartmentEntity>())
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ =>
                {
                    this.Departments.Clear();
                    this.Departments.AddRange(db.Departments.ToList());
                });
            this.Departments.Clear();
            this.Departments.AddRange(db.Departments.ToList());
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void ShowDepartment() {
            _host.AddPageAsync(
                    new DepartmentFormToken(this.SelectedDepartment.Name, this.SelectedDepartment));
        }

        private void DeleteDepartment() {
            if (this.SelectedDepartment == null) {
                return;
            }
            var messageBoxResult = MessageBox.Show(Localization["Вы уверены, что хотите удалить факультет?"],
                Localization["Подтверждение удаления"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult != MessageBoxResult.Yes)
            {
                return;
            }
            _db.Departments.Remove(this.SelectedDepartment);
            _db.SaveChangesAsync();
        }

        public ObservableCollection<DepartmentEntity> Departments { get; set; } =
            new ObservableCollection<DepartmentEntity>();

        [Reactive] public DepartmentEntity SelectedDepartment { get; set; }

        public ButtonConfig DeleteMenuButtonConfig { get; set; }
        public ButtonConfig ShowMenuButtonConfig { get; set; }
    }
}
