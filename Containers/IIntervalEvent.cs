using System;

namespace TeacherAssistant.Services
{
    public interface IIntervalEvent
    {
        StartPoint RelativelyTo { get; }
        TimeSpan TimeDiff { get; }
    }
}
