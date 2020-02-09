using System.Collections.Generic;

namespace Model.Models {
    public class StudentEqualityComparer : IEqualityComparer<StudentEntity> {
        public bool Equals(StudentEntity x, StudentEntity y) {
            if (x == null && y == null) {
                return true;
            }

            if (x == null) {
                return false;
            }
            if (y == null) {
                return false;
            }

            return x.Id == y.Id;
        }

        public int GetHashCode(StudentEntity obj) {
            return obj.Id.GetHashCode();
        }
    }
}