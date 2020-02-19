using System;
using Model;
using TeacherAssistant.Services;

namespace TeacherAssistant
{
    public class AlarmEvent : IIntervalEvent
    {
        public AlarmEvent(AlarmEntity alarm)
        {
            this.Alarm = alarm;
            this.TimeDiff = alarm.SinceLessonStart ?? default;
        }

        public AlarmEntity Alarm { get; }
        public StartPoint RelativelyTo { get; } = StartPoint.Start;
        public TimeSpan TimeDiff { get; }
    }
}
