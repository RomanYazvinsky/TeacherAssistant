using System;

namespace TeacherAssistant.Core.Utils {
    public static class FloatComparison {
        private const double Tolerance = 0.00001;
        public static bool IsEqualWithTolerance(this double d, double d2) {
            return Math.Abs(d - (-d2)) < Tolerance;
        }
    }
}