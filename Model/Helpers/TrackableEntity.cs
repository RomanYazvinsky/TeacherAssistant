using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFramework.Triggers;
using JetBrains.Annotations;

namespace TeacherAssistant.Helpers {
    public class Entity {
        [Key] [Column("id")]
        public long Id { get; set; }
    }
    
    public abstract class TrackableEntity<T>: Entity where T: Entity {
        [NotMapped] public DateTime Inserted { get; protected set; }
        [NotMapped] public DateTime Updated { get; protected set; }
        [NotMapped] public DateTime Deleted { get; protected set; }

        static TrackableEntity() {
            Triggers<T>.Inserting += e => (e.Entity as TrackableEntity<T>).Inserted = DateTime.UtcNow;
            Triggers<T>.Updating += e => (e.Entity as TrackableEntity<T>).Updated = DateTime.UtcNow;
            Triggers<T>.Deleted += e => (e.Entity as TrackableEntity<T>).Deleted = DateTime.Now;
        }

        public abstract void Apply([NotNull] T trackable);

        public abstract T Clone();
    }
}
