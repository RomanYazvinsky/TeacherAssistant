using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using JetBrains.Annotations;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models {
    [Table("GROUP")]
    public class GroupEntity : TrackableEntity<GroupEntity> {
        private DepartmentEntity _department;
        private string _name;
        private const string ExpirationDateTemplate = "yyyy-MM-dd";
        private const string ExpirationDateTime = "T00:00:00";

        public GroupEntity() {
        }

        public GroupEntity(GroupEntity entity) {
            Apply(entity);
        }

        [Column("type_id")] public long? _GroupTypeId { get; set; } = null;

        [ForeignKey("_GroupTypeId")] public virtual GroupTypeModel _GroupType { get; set; }
        [ForeignKey("_PraepostorId")] public virtual StudentEntity _Praepostor { get; set; }
        [Column("praepostor_id")] public long? _PraepostorId { get; set; }
        [CanBeNull] public virtual ICollection<StudentEntity> Students { get; set; } = new List<StudentEntity>();
        public virtual ICollection<StreamEntity> Streams { get; set; } = new List<StreamEntity>();
        [Column("active")] public long? _IsActive { get; set; } = 0;
        [Column("expiration_date")] public string _ExpirationDate { get; set; }

        [Column("name")]
        public string Name {
            get => _name;
            set {
                if (string.Equals(value, _name, StringComparison.Ordinal))
                    return;
                _name = value;
            }
        }

        [Column("department_id")] public long? _DepartmentId { get; set; }

        [ForeignKey("_DepartmentId")]
        [CanBeNull]
        public virtual DepartmentEntity Department {
            get => _department;
            set {
                if (Equals(value, _department))
                    return;
                _department = value;
                this._DepartmentId = value?.Id;
            }
        }

        [NotMapped]
        public StudentEntity Chief {
            get => this._Praepostor;
            set {
                if (Equals(value, this._Praepostor))
                    return;
                this._Praepostor = value;
                this._PraepostorId = value?.Id;
            }
        }

        [NotMapped]
        public bool IsActive {
            get => this._IsActive.HasValue && this._IsActive > 0;
            set => this._IsActive = value ? 1 : 0;
        }

        [NotMapped]
        public DateTime? ExpirationDate {
            get {
                if (this._ExpirationDate == null) {
                    return null;
                }

                return DateTime.TryParseExact
                       (
                           this._ExpirationDate.Substring(0, 10),
                           ExpirationDateTemplate,
                           CultureInfo.InvariantCulture,
                           DateTimeStyles.None,
                           out var result
                       )
                           ? result
                           : default(DateTime?);
            }
            set {
                if (value == null) {
                    this._ExpirationDate = null;
                }
                else {
                    this._ExpirationDate = value.Value.ToString(ExpirationDateTemplate) + ExpirationDateTime;
                }

            }
        }

        public sealed override void Apply(GroupEntity entity) {
            this.Name = entity.Name;
            this.Chief = entity.Chief;
            this.Department = entity.Department;
            this.ExpirationDate = entity.ExpirationDate;
            this.IsActive = entity.IsActive;
            this._DepartmentId = entity._DepartmentId;
            this._PraepostorId = entity._PraepostorId;
            this.Students = entity.Students;
            this.Streams = entity.Streams;
            this.Id = entity.Id;
        }

        public override GroupEntity Clone()
        {
            return new GroupEntity(this);
        }
    }
}
