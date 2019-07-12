using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Model.Models;

namespace TeacherAssistant {
    public class LessonTimerService {
        private List<LessonModel> _timerSchedule;
        private LinkedList<(TimeSpan, Action<LessonModel>)> _scheduledActions;
        private Timer _activeTimer;

        public void Init(IEnumerable<LessonModel> timerSchedule,
            Dictionary<TimeSpan, Action<LessonModel>> sinceStartDo) {
            Stop();
            _timerSchedule = timerSchedule
                .Where(timer => timer.Date.HasValue && timer.Date.Value.Date >= DateTime.Today)
                .OrderBy(timer => timer.Date).ThenBy(model => model.Schedule.Begin).ToList();
            _scheduledActions = new LinkedList<(TimeSpan, Action<LessonModel>)>(sinceStartDo.OrderBy(pair => pair.Key)
                .Select(pair => (pair.Key, pair.Value)).ToList());
        }

        public DateTime? Start() {
            return Start(new TimeSpan(2, 0, 0, 0));
        }

        public DateTime? Start(TimeSpan queuePeriod) {
            if (_timerSchedule == null) {
                return null;
            }

            var now = DateTime.Now;
            var queue = new LinkedList<LessonModel>(_timerSchedule.Where(model => model.Date < now.Date + queuePeriod));
            var firstLessonToAlarm = queue.FirstOrDefault(model =>
                model.Date.Value + model.Schedule.Begin > now ||
                (model.Date.Value + model.Schedule.Begin < now && model.Date.Value + model.Schedule.End > now));
            if (firstLessonToAlarm == null) {
                return null;
            }

            var lessonNode = queue.Find(firstLessonToAlarm);
            var firstAlarm = _scheduledActions.FirstOrDefault(tuple =>
                firstLessonToAlarm.Date.Value + firstLessonToAlarm.Schedule.Begin + tuple.Item1 > now);
            if (firstAlarm == default) {
                firstAlarm = _scheduledActions.First.Value;
                lessonNode = lessonNode.Next;
                if (lessonNode == null) {
                    return null;
                }
            }

            StartTimer(lessonNode, _scheduledActions.Find(firstAlarm));
            return lessonNode.Value.Date + lessonNode.Value.Schedule.Begin + firstAlarm.Item1;
        }

        private void StartTimer(LinkedListNode<LessonModel> model,
            LinkedListNode<(TimeSpan, Action<LessonModel>)> scheduledAction) {
            _activeTimer = new Timer(state => {
                    if (_activeTimer == null) {
                        return;
                    }

                    _activeTimer = null;
                    var (lesson, action) = ((LinkedListNode<LessonModel>,
                        LinkedListNode<(TimeSpan, Action<LessonModel>)>)) state;
                    action.Value.Item2(lesson.Value);
                    var isLastScheduledAction = action.Next == null;
                    if (!isLastScheduledAction) {
                        StartTimer(model, action.Next);
                        return;
                    }

                    var nextLesson = lesson.Next;
                    if (nextLesson == null) {
                        return;
                    }

                    StartTimer(nextLesson, _scheduledActions.First);
                }, (model, scheduledAction),
                ((model.Value.Date + model.Value.Schedule.Begin + scheduledAction.Value.Item1) - DateTime.Now).Value,
                Timeout.InfiniteTimeSpan);
        }

        public void Stop() {
            if (_activeTimer == null) return;
            _activeTimer.Dispose();
            _activeTimer = null;
        }
    }
}