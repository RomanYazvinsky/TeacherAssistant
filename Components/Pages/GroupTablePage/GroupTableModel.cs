using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Containers;
using DynamicData;
using Model.Models;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.GroupForm;

namespace TeacherAssistant.GroupTable {
    public class GroupTableModel : AbstractModel<GroupTableModel> {
        private readonly LocalDbContext _db;
        private readonly IPageHost _host;
        private static readonly string LocalizationKey = "page.group.table";

        public GroupTableModel(LocalDbContext db, WindowPageHost host) {
            _db = db;
            _host = host;
            this.DeleteMenuButtonConfig = new ButtonConfig {
                Command = new CommandHandler(DeleteGroup)
            };
            this.ShowMenuButtonConfig = new ButtonConfig {
                Command = new CommandHandler(ShowGroup)
            };
            Observable.Merge(WhenAdded<GroupEntity>(), WhenRemoved<GroupEntity>(), WhenUpdated<GroupEntity>())
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ =>
                {
                    this.Groups.Clear();
                    this.Groups.AddRange(db.Groups.ToList());
                });
            this.Groups.Clear();
            this.Groups.AddRange(db.Groups.ToList());
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void ShowGroup() {
            _host.AddPageAsync<GroupFormModule, GroupFormToken>(
                    new GroupFormToken(this.SelectedGroupEntity.Name, this.SelectedGroupEntity));
        }

        private void DeleteGroup() {
            if (this.SelectedGroupEntity == null) {
                return;
            }
            var messageBoxResult = MessageBox.Show(Localization["Вы уверены, что хотите удалить группу?"],
                Localization["Подтверждение удаления"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult != MessageBoxResult.Yes)
            {
                return;
            }
            _db.Groups.Remove(this.SelectedGroupEntity);
            _db.SaveChangesAsync();
        }

        public ObservableCollection<GroupEntity> Groups { get; set; } =
            new ObservableCollection<GroupEntity>();

        [Reactive] public GroupEntity SelectedGroupEntity { get; set; }

        public ButtonConfig DeleteMenuButtonConfig { get; set; }
        public ButtonConfig ShowMenuButtonConfig { get; set; }
    }
}
