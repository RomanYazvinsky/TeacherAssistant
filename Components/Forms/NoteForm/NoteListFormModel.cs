using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Containers;
using Containers.Annotations;
using DynamicData;
using DynamicData.Binding;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Extensions;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using ValidationContext = ReactiveUI.Validation.Contexts.ValidationContext;

namespace TeacherAssistant.Forms.NoteForm
{
    public class NoteViewModel : ViewModelBase
    {
        private string _description;

        public NoteEntity Note { get; }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public NoteViewModel(NoteEntity note)
        {
            Note = note;
            _description = note.Description ?? "";
        }
    }

    public class NoteFormModel : AbstractModel, IValidatableViewModel
    {
        private readonly NoteListFormToken _token;
        private readonly LocalDbContext _context;
        private const string LocalizationKey = "note.form";

        public NoteFormModel(NoteListFormToken token, LocalDbContext context)
        {
            _token = token;
            _context = context;
            this.ValidationRule(
                model => model.Text,
                s => !string.IsNullOrWhiteSpace(s),
                Localization["Невалидное значение"]
            );
            this.SaveButtonConfig = new ButtonConfig
            {
                Command = ReactiveCommand.Create(Save, this.IsValid()),
                Text = Localization["Сохранить"]
            };
            this.AddNoteButtonConfig = new ButtonConfig
            {
                Command = ReactiveCommand.Create(AddNote),
                Text = Localization["Добавить"]
            };
            this.RemoveNoteButtonConfig = new ButtonConfig
            {
                Command = ReactiveCommand.Create(RemoveNote,
                        this.WhenAnyValue(model => model.SelectedNote).Select(model => model != null)
                ),
                Text = Localization["Удалить"]
            };
            this.Notes.AddRange(token.Notes.Select(note => new NoteViewModel(note)).ToList());
            this.IsEditorAvailable = this.Notes.Count > 0;
            this.SelectedNote = token.SelectedNote == null
                ? (Notes.Count > 0 ? Notes[0] : null)
                : Notes.FirstOrDefault(noteModel => noteModel.Note.Id.Equals(token.SelectedNote.Id));
            this.WhenActivated(disp =>
            {
                this.IsValid()
                    .Subscribe(b => this.IsNoteListSelectable = b)
                    .DisposeWith(disp);
                Notes.ToObservableChangeSet()
                    .ToCollection()
                    .Subscribe(models => this.IsEditorAvailable = models.Count > 0)
                    .DisposeWith(disp);
                this.WhenAnyValue(model => model.SelectedNote)
                    .Where(NotNull)
                    .Subscribe(model => this.Text = model.Description)
                    .DisposeWith(disp);
                this.WhenAnyValue(model => model.Text)
                    .Subscribe(s =>
                    {
                        if (this.SelectedNote == null)
                        {
                            return;
                        }

                        this.SelectedNote.Description = s;
                    })
                    .DisposeWith(disp);
            });
        }

        protected override string GetLocalizationKey()
        {
            return LocalizationKey;
        }

        public ButtonConfig SaveButtonConfig { get; set; }
        public ButtonConfig AddNoteButtonConfig { get; set; }
        public ButtonConfig RemoveNoteButtonConfig { get; set; }

        [Reactive] public bool IsEditorAvailable { get; set; }
        [Reactive] public bool IsNoteListSelectable { get; set; }

        public ObservableCollection<NoteViewModel> Notes { get; } = new ObservableCollection<NoteViewModel>();

        [Reactive] [CanBeNull] public NoteViewModel SelectedNote { get; set; }

        [Reactive] public string Text { get; set; }


        private void AddNote()
        {
            var note = _token.NoteFactory();
            note.CreationDate = DateTime.Now;
            var noteViewModel = new NoteViewModel(note);
            this.Notes.Add(noteViewModel);
            this.SelectedNote = noteViewModel;
        }

        private void RemoveNote()
        {
            var index = Notes.IndexOf(this.SelectedNote);
            Notes.Remove(this.SelectedNote);
            if (index == 0)
            {
                return;
            }

            this.SelectedNote = Notes.Count > index ? Notes[index] : Notes[index - 1];
        }

        private void Save()
        {
            foreach (var noteViewModel in this.Notes)
            {
                noteViewModel.Note.Description = noteViewModel.Description;
            }

            var removedIds = _token.Notes
                .Where(note => note.Id != default && Notes.All(model => model.Note.Id != note.Id))
                .Select(entity => entity.Id)
                .ToList();
            var notesToRemove = _context.Set<NoteEntity>().Where(entity => removedIds.Contains(entity.Id));
            _context.Set<NoteEntity>().RemoveRange(notesToRemove);

            var added = this.Notes.Where(model => model.Note.Id == default).Select(model => model.Note);
            _context.Set<NoteEntity>().AddRange(added);

            var changed = this.Notes
                .Where(model => model.Note.Id != default)
                .Select(model => model.Note)
                .ToDictionary(entity => entity.Id);
            var changedIds = changed.Keys.ToList();
            var noteEntities = _context.Set<NoteEntity>().Where(entity => changedIds.Contains(entity.Id)).ToList();
            noteEntities.ForEach(entity => entity.Apply(changed[entity.Id]));
            _context.SaveChangesAsync();
            _token.Deactivate();
        }


        private void UpdateDescription(IEnumerable<NoteViewModel> models)
        {
            foreach (var noteViewModel in models)
            {
                noteViewModel.Note.Description = noteViewModel.Description;
            }
        }

        public ValidationContext ValidationContext { get; } = new ValidationContext();
    }
}
