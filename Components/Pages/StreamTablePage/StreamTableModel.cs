using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.StreamForm;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.StreamTable {
    public class StreamTableModel : AbstractModel<StreamTableModel> {
        private readonly LocalDbContext _db;
        private readonly IComponentHost _host;
        private static readonly string LocalizationKey = "page.stream.table";

        public StreamTableModel(LocalDbContext db, WindowComponentHost host) {
            _db = db;
            _host = host;
            this.DeleteHandler = ReactiveCommand.Create(DeleteStream);
            this.ShowHandler = ReactiveCommand.Create(ShowStream);
            Observable.Merge(WhenAdded<StreamEntity>(), WhenRemoved<StreamEntity>(), WhenUpdated<StreamEntity>())
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ =>
                {
                    this.Streams.Clear();
                    this.Streams.AddRange(db.Streams.ToList());
                });
            this.Streams.Clear();
            this.Streams.AddRange(db.Streams.ToList());
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void ShowStream() {
            _host.AddPageAsync(new StreamFormToken("Поток", this.SelectedStream));
        }

        private async Task DeleteStream() {
            if (this.SelectedStream == null) {
                return;
            }
            var messageBoxResult = MessageBox.Show(Localization["Вы уверены, что хотите удалить поток?"],
                Localization["Подтверждение удаления"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult != MessageBoxResult.Yes)
            {
                return;
            }
            _db.Streams.Remove(this.SelectedStream);
            await _db.SaveChangesAsync();
        }

        public ObservableCollection<StreamEntity> Streams { get; set; } =
            new ObservableCollection<StreamEntity>();

        [Reactive] public StreamEntity SelectedStream { get; set; }

        public ICommand DeleteHandler { get; set; }
        public ICommand ShowHandler { get; set; }
    }
}
