using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Models;
using TeacherAssistant.Pages.StudentViewPage;

namespace TeacherAssistant.StudentViewPage {
    public class StudentAttestationExamView {
        private readonly StudentViewPageModel _model;

        public StudentAttestationExamView(StudentLessonEntity studentLesson, StudentViewPageModel model, int order) {
            _model = model;
            this.StudentLesson = studentLesson;
            this.Header = LocalizationContainer.Interpolate("page.student.view.attestation.header.label", order);
        }

        public StudentLessonEntity StudentLesson { get; }

        public string Header { get; set; }

        public string Mark {
            get => this.StudentLesson.Mark;
            set {
                var valid = false;
                switch (value) {
                    case "":
                    case "+":
                    case "-": {
                        valid = true;
                        break;
                    }

                    default: {
                        if (int.TryParse(value, out var i))
                            if (i >= 0 || i <= 10)
                                valid = true;

                        break;
                    }
                }

                if (!valid)
                    return;
                this.StudentLesson.Mark = value;
                _model.UpdateExamMark();
            }
        }
    }
}