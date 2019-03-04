using System;

namespace TeacherAssistant
{
    public class Effect<T>
    {
        public string Id { get; }
        private Predicate<T> _filter;
        private Action<T> _action;

        public Effect(string id, Predicate<T> filter)
        {
            Id = id;
            _filter = filter;
        }

        public Effect(string id, Action<T> action)
        {
            Id = id;
            _action = action;
        }

        public bool Execute(T param)
        {
            if (_action != null)
            {
                _action(param);
                return true;
            }

            if (_filter != null)
            {
                return _filter(param);
            }
            throw new NotImplementedException(Id);
        }
    }
}