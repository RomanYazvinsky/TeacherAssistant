using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFramework.Triggers;
using JetBrains.Annotations;

namespace TeacherAssistant.Helpers {
    public class Entity {
        [Key] [Column("id")]
        public virtual long Id { get; set; }
    }
    
    public abstract class ATrackable<T>: Entity where T: Entity {
        [NotMapped] public DateTime Inserted { get; protected set; }
        [NotMapped] public DateTime Updated { get; protected set; }
        [NotMapped] public DateTime Deleted { get; protected set; }

        static ATrackable() {
            Triggers<T>.Inserting += e => (e.Entity as ATrackable<T>).Inserted = DateTime.UtcNow;
            Triggers<T>.Updating += e => (e.Entity as ATrackable<T>).Updated = DateTime.UtcNow;
            Triggers<T>.Deleted += e => (e.Entity as ATrackable<T>).Deleted = DateTime.Now;
        }

        public abstract void Apply([NotNull] T trackable);

        public abstract T Clone();
    }
}
