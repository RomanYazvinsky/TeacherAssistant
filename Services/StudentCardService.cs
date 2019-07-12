using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using TeacherAssistant.Dao;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.Components {
    public class StudentCardService {
        private ISerialUtil _serialUtil;
        public ObservableCollection<StudentCard> ReadStudentCards { get; } = new ObservableCollection<StudentCard>();
        public IObservable<StudentCard> ReadStudentCard { get; private set; }

        public StudentCardService(ISerialUtil serialUtil) {
            _serialUtil = serialUtil;
            GeneralDbContext.DatabaseChanged += (sender, s) => Init();
            Init();
        }

        private void Init() {
            this.ReadStudentCard = _serialUtil.OnRead()
                                              .Select(strings => new StudentCard(strings));
            this.ReadStudentCard.Subscribe(Save);
        }

        private void Save(StudentCard card) {
            this.ReadStudentCards.Add(card);
        }
    }
}