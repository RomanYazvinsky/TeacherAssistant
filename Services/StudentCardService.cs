using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.Components {
    public class StudentCardService : IDisposable {
        private IDisposable _subscription;
        public ObservableCollection<StudentCard> ReadStudentCards { get; } = new ObservableCollection<StudentCard>();
        public IObservable<StudentCard> ReadStudentCard { get; }

        public StudentCardService(SerialUtil serialUtil) {
            this.ReadStudentCard = serialUtil.ReadData
                .Select(strings => new StudentCard(strings));
            _subscription = this.ReadStudentCard.Subscribe(Save);
        }


        private void Save(StudentCard card) {
            this.ReadStudentCards.Add(card);
        }

        public void Dispose() {
            _subscription.Dispose();
        }
    }
}