using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using TeacherAssistant.Dao;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.Components {
    public class StudentCardService: IDisposable {
        private SerialUtil _serialUtil;
        private IDisposable _subscription;
        public ObservableCollection<StudentCard> ReadStudentCards { get; } = new ObservableCollection<StudentCard>();
        public IObservable<StudentCard> ReadStudentCard { get; private set; }

        public StudentCardService(SerialUtil serialUtil) {
            _serialUtil = serialUtil;
            LocalDbContext.DatabaseChanged += (sender, s) => Init();
            Init();
        }

        private void Init() {
            this.ReadStudentCard = _serialUtil.ReadData
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
