using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public class NoteViewModel : ViewModelBase, IDisposable
    {
        private string _description;
        private bool _isSelected;
        private bool _isNotEditable = true;
        private readonly BehaviorSubject<bool> _isValid = new BehaviorSubject<bool>(false);
        public NoteEntity Note { get; }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                this._isValid.OnNext(!string.IsNullOrWhiteSpace(value));
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                this.IsNotEditable = !value;
                OnPropertyChanged();
            }
        }

        public bool IsNotEditable
        {
            get => _isNotEditable;
            set
            {
                if (value == _isNotEditable) return;
                _isNotEditable = value;
                OnPropertyChanged();
            }
        }

        public IObservable<bool> IsValid => _isValid;

        public NoteViewModel(NoteEntity note)
        {
            Note = note;
            Description = note.Description ?? "";
        }

        public void Dispose()
        {
            _isValid?.Dispose();
        }
    }

    public class NoteFormModel : AbstractModel<NoteFormModel>
    {
        private readonly NoteListFormToken _token;
        private readonly LocalDbContext _context;
        private const string LocalizationKey = "note.form";

        public NoteFormModel(NoteListFormToken token, LocalDbContext context)
        {
            _token = token;
            _context = context;
            this.SaveButtonConfig = new ButtonConfig
            {
                Command = ReactiveCommand.Create(Save, this.WhenAnyValue(model => model.IsValid)),
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
                IDisposable subscription = null;
                NoteViewModel prevSelectedVm = null;
                this.WhenAnyValue(model => model.SelectedNote)
                    .Subscribe(noteVm =>
                    {
                        subscription?.Dispose();
                        if (prevSelectedVm != null)
                        {
                            prevSelectedVm.IsSelected = false;
                        }
                        prevSelectedVm = noteVm;
                        if (noteVm == null)
                        {
                            return;
                        }
                        noteVm.IsSelected = true;
                        subscription = noteVm.IsValid.Subscribe(b =>
                        {
                            this.IsValid = b;
                        });
                    }).DisposeWith(disp);
                Notes.ToObservableChangeSet()
                    .ToCollection()
                    .Subscribe(models => this.IsEditorAvailable = models.Count > 0)
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
        [Reactive] public bool IsValid { get; set; }

        public ObservableCollection<NoteViewModel> Notes { get; } = new ObservableCollection<NoteViewModel>();

        [Reactive] [CanBeNull] public NoteViewModel SelectedNote { get; set; }



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
            this.SelectedNote?.Dispose();
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


        public override void Dispose()
        {
            base.Dispose();
            foreach (var noteViewModel in this.Notes)
            {
                noteViewModel.Dispose();
            }
        }
    }
}
