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
            WhenAdded<GroupModel>().ObserveOn(RxApp.MainThreadScheduler).Subscribe(models =>  this.Groups.AddRange(models));
            WhenRemoved<GroupModel>().ObserveOn(RxApp.MainThreadScheduler).Subscribe(models =>  this.Groups.RemoveRange(models));
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public override Task Init() {
            Select<object>("").Subscribe(o => {
                this.Groups.Clear();
                this.Groups.AddRange(_db.GroupModels.ToList());
            });
         return Task.CompletedTask;
        }

        private void ShowGroup() {
            var id = this.PageService.OpenPage
                ("Modal", new PageProperties {PageType = typeof(GroupForm)});
            StoreManager.Publish(this.SelectedGroupModel, id, "GroupChange");
        }

        private void DeleteGroup() {
            if (this.SelectedGroupModel == null) {
                return;
            }

            _db.GroupModels.Remove(this.SelectedGroupModel);
            _db.SaveChangesAsync();
            this.Groups.Remove(this.SelectedGroupModel);
        }

        public ObservableRangeCollection<GroupModel> Groups { get; set; } =
            new WpfObservableRangeCollection<GroupModel>();

        [Reactive] public GroupModel SelectedGroupModel { get; set; }

        public ButtonConfig DeleteMenuButtonConfig { get; set; }
        public ButtonConfig ShowMenuButtonConfig { get; set; }
    }
}