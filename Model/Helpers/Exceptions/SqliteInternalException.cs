using System;

namespace TeacherAssistant.Helpers.Exceptions {
    [Serializable]
    public class SqliteInternalException: Exception {
        public SqliteInternalException(): base("SQLiteFactory failed to create connection: returned null.") {
            
        }
    }
}