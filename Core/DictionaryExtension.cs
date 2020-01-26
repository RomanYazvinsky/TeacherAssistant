using System.Collections.Generic;
using System.Collections.Immutable;

namespace TeacherAssistant.State
{
    public static class DictionaryExtension
    {
        public static T GetOrDefault<T>(this Dictionary<string, T> dictionary, string id)
        {
            return dictionary.ContainsKey(id) ? dictionary[id] : default;
        }
        public static T GetOrDefault<T>(this ImmutableDictionary<string, object> dictionary, string id)
        {
            return (T) (dictionary.ContainsKey(id) ? dictionary[id] : default);
        }
    }
}