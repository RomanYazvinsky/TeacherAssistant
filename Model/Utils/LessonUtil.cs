namespace TeacherAssistant.Utils {
    public static class LessonUtil {
        public const int MinimalMark = 0;
        public const int MaximalMark = 10;

        public static bool IsValueValidMark(string value) {
            var isNumber = double.TryParse(value, out var markAsNumber);
            if (!isNumber) {
                return false;
            }

            return markAsNumber >= MinimalMark && markAsNumber <= MaximalMark;
        }
    }
}