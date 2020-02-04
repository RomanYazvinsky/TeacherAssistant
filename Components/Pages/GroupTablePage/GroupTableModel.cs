using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Forms.GroupForm;

namespace TeacherAssistant.GroupTable {
    public class GroupTableModel : AbstractModel {
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
            WhenAdded<GroupEntity>().ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(models => this.Groups.AddRange(models));
            WhenRemoved<GroupEntity>().ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(models => this.Groups.RemoveRange(models));
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

            _db.Groups.Remove(this.SelectedGroupEntity);
            _db.SaveChangesAsync();
            this.Groups.Remove(this.SelectedGroupEntity);
        }

        public ObservableRangeCollection<GroupEntity> Groups { get; set; } =
            new WpfObservableRangeCollection<GroupEntity>();

        [Reactive] public GroupEntity SelectedGroupEntity { get; set; }

        public ButtonConfig DeleteMenuButtonConfig { get; set; }
        public ButtonConfig ShowMenuButtonConfig { get; set; }
    }
}