using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Forms.DisciplineForm
{
    public class DisciplineFormModel : AbstractModel<DisciplineFormModel>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ModuleActivation<DisciplineFormToken> _activation;
        private readonly LocalDbContext _context;
        private DisciplineEntity _detachedEntity;
        private bool _isCreationMode = false;

        public DisciplineFormModel(ModuleActivation<DisciplineFormToken> activation, LocalDbContext context)
        {
            _activation = activation;
            _context = context;
            this.ValidationRule(
                model => model.DisciplineName,
                name => !string.IsNullOrWhiteSpace(name),
                Localization["Неверное имя!"]
            );
            this.WhenActivated(c =>
            {
                this.IsValid().Subscribe(valid => this.IsValid = valid).DisposeWith(c);
            });
            this.SaveHandler = ReactiveCommand.Create(SaveAsync);
            Init(activation.Token.Discipline);
        }

        private void Init([NotNull] DisciplineEntity discipline)
        {
            _isCreationMode = discipline.Id == default;
            _detachedEntity = _isCreationMode ? discipline : discipline.Clone();
            this.DisciplineName = _detachedEntity.Name;
            this.DisciplineDescription = _detachedEntity.Description;
        }

        [Reactive] public string DisciplineName { get; set; }
        [Reactive] public string DisciplineDescription { get; set; }
        [Reactive] public bool IsValid { get; set; }
        public ICommand SaveHandler { get; }

        private async Task SaveAsync()
        {
            var persistentEntity = _isCreationMode
                ? _detachedEntity
                : await _context.Disciplines.FindAsync(_detachedEntity.Id);
            if (persistentEntity == null)
            {
                var exception = new Exception("Cannot save discipline: item not found");
                Logger.Log(LogLevel.Error, exception);
                throw exception;
            }

            persistentEntity.Name = this.DisciplineName;
            persistentEntity.Description = this.DisciplineDescription;
            if (_isCreationMode)
            {
                _context.Disciplines.Add(persistentEntity);
            }
            await _context.SaveChangesAsync();
            _activation.Deactivate();
        }

        protected override string GetLocalizationKey()
        {
            return "discipline.form";
        }
    }
}
