using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models
{
    [Table("DISCIPLINE")]
    public class DisciplineEntity: ATrackable<DisciplineEntity>
    {

        public DisciplineEntity()
        {

        }
        public DisciplineEntity(DisciplineEntity discipline)
        {
            Apply(discipline);
        }
        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("create_date")] public string _CreationDate { get; set; }

        [Column("active")] public long? _IsActive { get; set; }

        [Column("expiration_date")] public string _ExpirationDate { get; set; }
        public sealed override void Apply(DisciplineEntity trackable)
        {
            this.Id = trackable.Id;
            this.Description = trackable.Description;
            this.Name = trackable.Name;
            this._CreationDate = trackable._CreationDate;
            this._IsActive = trackable._IsActive;
            this._ExpirationDate = trackable._ExpirationDate;
        }

        public override DisciplineEntity Clone()
        {
            return new DisciplineEntity(this);
        }
    }
}
