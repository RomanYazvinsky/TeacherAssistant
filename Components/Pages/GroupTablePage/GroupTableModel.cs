using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Forms.GroupForm;
using TeacherAssistant.State;

namespace TeacherAssistant.GroupTable {
    public class GroupTableModel : AbstractModel {
        private static readonly string LocalizationKey = "page.group.table";

        public GroupTableModel(string id) : base(id) {
            this.DeleteMenuButtonConfig = new ButtonConfig {
                Command = new CommandHandler(DeleteGroup)
            };
            this.ShowMenuButtonConfig = new ButtonConfig {
                Command = new CommandHandler(ShowGroup)
            };
            WhenAdded<GroupEntity>().ObserveOn(RxApp.MainThreadScheduler).Subscribe(models =>  this.Groups.AddRange(models));
            WhenRemoved<GroupEntity>().ObserveOn(RxApp.MainThreadScheduler).Subscribe(models =>  this.Groups.RemoveRange(models));
            Select<object>("").Subscribe(o => {
                this.Groups.Clear();
                this.Groups.AddRange(_db.Groups.ToList());
            });
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }
        
        private void ShowGroup() {
            var id = this.PageService.OpenPage("Modal", new PageProperties<GroupForm>());
            StoreManager.Publish(this.SelectedGroupEntity, id, "GroupChange");
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