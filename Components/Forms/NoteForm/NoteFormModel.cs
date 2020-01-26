using System;
using System.Data.Entity;
using System.Reactive.Linq;
using Containers;
using Model.Models;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Forms.NoteForm {
    public class NoteFormModel : AbstractModel {
        private const string LocalizationKey = "note.form";
        private NoteEntity _originalNote;
        public NoteFormModel(string id) {
            this.SaveButtonConfig = new ButtonConfig {
                Command = new CommandHandler(Save),
                Text = Localization["Сохранить"]
            };
            // Select<NoteEntity>(this.Id, "Note").Where(NotNull).Subscribe(note => {
            //     _originalNote = note;
            //     this.Text = note.Description;
            // });
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }
        public ButtonConfig SaveButtonConfig { get; set; }

        [Reactive] public string Text { get; set; }

        private void Save() {
            if (string.IsNullOrWhiteSpace(this.Text)) {
                return;
            }
            if (_originalNote.Id == default) {
                _originalNote.Date = DateTime.Now;
                LocalDbContext.Instance.Set<NoteEntity>().Add(_originalNote);
            }
            _originalNote.Description = this.Text;
            // Database.SaveChangesAsync();
            // this._pageService.ClosePage(this.Id);
        }


    }
}