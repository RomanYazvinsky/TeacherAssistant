using System;

namespace TeacherAssistant.ReaderPlugin
{
    public interface ISerialUtil
    {
        event EventHandler<string> Connected;
        event EventHandler Disconnected;
        event EventHandler<string> ConnectionFailed;
        void Start();
        void Close();
        IObservable<string[]> OnRead();
    }
}