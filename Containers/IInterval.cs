using System;

namespace TeacherAssistant.Services
{
    public interface IInterval
    {
        DateTime StartDateTime { get; }
        DateTime EndDateTime { get; }
    }
}
