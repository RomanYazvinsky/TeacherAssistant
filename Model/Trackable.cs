using System;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFramework.Triggers;

namespace TeacherAssistant.Dao {
    public abstract class Trackable<T> where T: class {
        [NotMapped] public DateTime Inserted { get; protected set; }
        [NotMapped] public DateTime Updated { get; protected set; }
        [NotMapped] public DateTime Deleted { get; protected set; }

        static Trackable() {
            Triggers<T>.Inserting += e => (e.Entity as Trackable<T>).Inserted = DateTime.UtcNow;
            Triggers<T>.Updating += e => (e.Entity as Trackable<T>).Updated = DateTime.UtcNow;
            Triggers<T>.Deleted += e => (e.Entity as Trackable<T>).Deleted = DateTime.Now;
        }

        public abstract void Apply(T trackable);

        public abstract T Clone();
    }
}
