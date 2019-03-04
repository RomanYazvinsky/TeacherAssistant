using System;

namespace TeacherAssistant.ReaderPlugin
{
    public interface ISerialUtil
    {
        event EventHandler<string> DataReceivedSuccess;
        event EventHandler<string> DataReceivedError;
        event EventHandler<string> Connected;
        event EventHandler Disconnected;
        void Start();
        void Close();
    }
}