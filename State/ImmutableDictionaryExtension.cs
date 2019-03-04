using System.Collections.Immutable;

namespace TeacherAssistant.State
{
    public static class ImmutableDictionaryExtension
    {
        public static T GetOrDefault<T>(this ImmutableDictionary<string, DataContainer> dictionary, string id)
        {
            return dictionary.ContainsKey(id) ? dictionary[id].GetData<T>() : default(T);
        }
    }
}