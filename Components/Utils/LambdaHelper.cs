using System;

namespace TeacherAssistant.Utils {
    public static class LambdaHelper {
        public static bool NotNull<T>(T t) => t != null;
        public static bool NotNull<T, TV>((T, TV) t) => t.Item1 != null && t.Item2 != null;
        public static Tuple<T1, T2> ToTuple<T1, T2>(T1 t1, T2 t2) => new Tuple<T1, T2>(t1, t2);
    }
}