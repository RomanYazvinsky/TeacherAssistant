﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model.Models {
    [Table("GROUP")]
    public class GroupModel : Trackable<GroupModel>, INotifyPropertyChanged {
        private DepartmentModel _department;
        private string _name;
        private const string ExpirationDateTemplate = "yyyy-MM-dd";
        private const string ExpirationDateTime = "T00:00:00";
        [Key, Column("id")] public long Id { get; set; }


        public GroupModel() {
        }

        public GroupModel(GroupModel model) {
            Apply(model);
        }

        [Column("type_id")] public long? _GroupTypeId { get; set; } = null;

        [ForeignKey("_GroupTypeId")] public virtual GroupTypeModel _GroupType { get; set; }
        [ForeignKey("_PraepostorId")] public virtual StudentModel _Praepostor { get; set; }
        [Column("praepostor_id")] public long? _PraepostorId { get; set; }
        public virtual ICollection<StudentModel> Students { get; set; } = new List<StudentModel>();
        public virtual ICollection<StreamModel> Streams { get; set; } = new List<StreamModel>();
        [Column("active")] public long? _IsActive { get; set; } = 0;
        [Column("expiration_date")] public string _ExpirationDate { get; set; }

        [Column("name")]
        public string Name {
            get => _name;
            set {
                if (value == _name)
                    return;
                _name = value;
                OnPropertyChanged();
            }
        }

        [Column("department_id")] public long? _DepartmentId { get; set; }

        [ForeignKey("_DepartmentId")]
        public virtual DepartmentModel Department {
            get => _department;
            set {
                if (Equals(value, _department))
                    return;
                _department = value;
                this._DepartmentId = value?.Id;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public StudentModel Chief {
            get => this._Praepostor;
            set {
                if (Equals(value, this._Praepostor))
                    return;
                this._Praepostor = value;
                this._PraepostorId = value?.Id;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public bool IsActive {
            get => this._IsActive.HasValue && this._IsActive > 0;
            set {
                this._IsActive = value ? 1 : 0;
                OnPropertyChanged();
            }
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

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void Apply(GroupModel model) {
            this.Name = model.Name;
            this.Chief = model.Chief;
            this.Department = model.Department;
            this.ExpirationDate = model.ExpirationDate;
            this.IsActive = model.IsActive;
            this._DepartmentId = model._DepartmentId;
            this._PraepostorId = model._PraepostorId;
            this.Students = model.Students;
            this.Streams = model.Streams;
            this.Id = model.Id;
        }
    }
}