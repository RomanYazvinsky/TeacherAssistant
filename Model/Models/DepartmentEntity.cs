using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models {
    [Table("DEPARTMENT")]
    public class DepartmentEntity : ATrackable<DepartmentEntity> {
        public DepartmentEntity() {
        }

        public DepartmentEntity(DepartmentEntity department) {
            Apply(department);
        }

        [Column("name")] public string Name { get; set; }

        [Column("abbreviation")] public string Abbreviation { get; set; }

        public sealed override void Apply(DepartmentEntity trackable) {
            this.Id = trackable.Id;
            this.Name = trackable.Name;
            this.Abbreviation = trackable.Abbreviation;
        }

        public override DepartmentEntity Clone() {
            return new DepartmentEntity(this);
        }
    }
}