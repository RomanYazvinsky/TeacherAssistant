using System;

namespace TeacherAssistant.State
{
    public class EffectRegistration<T>
    {
        private Effect<T> _effect;

        public EffectRegistration(Effect<T> effect)
        {
            _effect = effect;
        }

        public void FilterPropertyChange(string id)
        {

        }

        public void BeforePropertyChange(string id)
        {
        }

        public void AfterPropertyChange(string id)
        {

        }
    }

    public class EffectLinker
    {
        public static EffectRegistration<T> With<T>(string id, Action<T> action)
        {
            return new EffectRegistration<T>(new Effect<T>(id, action));
        }

        public static EffectRegistration<T> With<T>(string id, Predicate<T> filter)
        {
            return new EffectRegistration<T>(new Effect<T>(id, filter));
        }

        public static EffectRegistration<T> With<T>(Effect<T> effect)
        {
            return new EffectRegistration<T>(effect);
        }
    }
}