using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Forms.DepartmentForm
{
    public class DepartmentFormModel : AbstractModel<DepartmentFormModel>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly DepartmentFormToken _token;
        private readonly LocalDbContext _context;
        private DepartmentEntity _detachedEntity;
        private bool _isCreationMode = false;

        public DepartmentFormModel(DepartmentFormToken token, LocalDbContext context)
        {
            _token = token;
            _context = context;
            this.ValidationRule(
                model => model.DepartmentName,
                name => !string.IsNullOrWhiteSpace(name),
                Localization["Неверное имя!"]
            );
            this.ValidationRule(
                model => model.DepartmentAbbreviation,
                name => !string.IsNullOrWhiteSpace(name),
                Localization["Неверная аббревиатура!"]
            );
            this.WhenActivated(c =>
            {
                this.IsValid().Subscribe(valid => this.IsValid = valid).DisposeWith(c);
            });
            this.SaveHandler = ReactiveCommand.Create(SaveAsync);
            Init(token.Department);
        }

        private void Init([NotNull] DepartmentEntity discipline)
        {
            _isCreationMode = discipline.Id == default;
            _detachedEntity = _isCreationMode ? discipline : discipline.Clone();
            this.DepartmentName = _detachedEntity.Name;
            this.DepartmentAbbreviation = _detachedEntity.Abbreviation;
        }

        [Reactive] public string DepartmentName { get; set; }
        [Reactive] public string DepartmentAbbreviation { get; set; }
        [Reactive] public bool IsValid { get; set; }
        public ICommand SaveHandler { get; }

        private async Task SaveAsync()
        {
            var persistentEntity = _isCreationMode
                ? _detachedEntity
                : await _context.Departments.FindAsync(_detachedEntity.Id);
            if (persistentEntity == null)
            {
                var exception = new Exception("Cannot save department: item not found");
                Logger.Log(LogLevel.Error, exception);
                throw exception;
            }

            persistentEntity.Name = this.DepartmentName;
            persistentEntity.Abbreviation = this.DepartmentAbbreviation;
            if (_isCreationMode)
            {
                _context.Departments.Add(persistentEntity);
            }

            await _context.SaveChangesAsync();
            _token.Deactivate();
        }

        protected override string GetLocalizationKey()
        {
            return "department.form";
        }
    }
}
