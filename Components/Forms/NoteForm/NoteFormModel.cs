using System;
using Containers;
using Model.Models;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Forms.NoteForm {
    public class NoteFormModel : AbstractModel {
        private readonly NoteFormToken _token;
        private readonly LocalDbContext _context;
        private const string LocalizationKey = "note.form";
        private NoteEntity _originalNote;

        public NoteFormModel(NoteFormToken token, LocalDbContext context) {
            _token = token;
            _context = context;
            this.SaveButtonConfig = new ButtonConfig {
                Command = new CommandHandler(Save),
                Text = Localization["Сохранить"]
            };
            this._originalNote = token.Entity;
            this.Text = token.Entity.Description;
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
                _originalNote.CreationDate = DateTime.Now;
                _context.Set<NoteEntity>().Add(_originalNote);
            }

            _originalNote.Description = this.Text;
            _context.SaveChangesAsync();
            _token.Deactivate();
        }
    }
}