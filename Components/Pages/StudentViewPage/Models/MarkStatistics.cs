namespace TeacherAssistant.StudentViewPage {
    public class MarkStatistics {
        public MarkStatistics(string mark, int occurrences) {
            this.Mark = mark;
            this.Occurrences = occurrences;
        }

        public string Mark { get; }
        public int Occurrences { get; }

        public string StatAsString => $"{this.Mark} - {this.Occurrences}";

        public int MarkAsNumber {
            get {
                if (!int.TryParse(this.Mark, out var markAsNumber))
                    return -1;
                if (markAsNumber >= StudentViewPageModel.MinAcceptableMark && markAsNumber <= StudentViewPageModel.MaxAcceptableMark)
                    return markAsNumber;

                return -1;
            }
        }
    }
}
