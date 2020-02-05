using System;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFramework.Triggers;

namespace TeacherAssistant.Dao {
    public abstract class Trackable<T> {
        [NotMapped] public DateTime Inserted { get; protected set; }
        [NotMapped] public DateTime Updated { get; protected set; }
        [NotMapped] public DateTime Deleted { get; protected set; }

        static Trackable() {
            Triggers<Trackable<T>>.Inserting += e => e.Entity.Inserted = DateTime.UtcNow;
            Triggers<Trackable<T>>.Updating += e => e.Entity.Updated = DateTime.UtcNow;
            Triggers<Trackable<T>>.Deleted += e => e.Entity.Deleted = DateTime.Now;
        }

        public abstract void Apply(T trackable);

        public abstract T Clone();
    }
}
