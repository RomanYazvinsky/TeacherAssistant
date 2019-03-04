namespace TeacherAssistant.State
{
    public class DataContainer
    {
        private object _data;

        public DataContainer(object data)
        {
            _data = data;
        }

        public T GetData<T>()
        {
            return (T)_data;
        }

        public void SetData<T>(T value)
        {
            _data = value;
        }
    }

}