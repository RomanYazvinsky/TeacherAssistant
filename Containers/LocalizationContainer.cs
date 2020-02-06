using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace TeacherAssistant.ComponentsImpl
{
    public class LocalizationContainer : SafeDictionary<string>
    {
        private readonly Dictionary<string, Dictionary<string, string>> _languageResources =
            new Dictionary<string, Dictionary<string, string>>();

        private static LocalizationContainer _instance;
        public static LocalizationContainer Localization => _instance ?? (_instance = new LocalizationContainer());

        public static string Interpolate(string key, params object[] values) {
            return string.Format(Localization[key], values);
        }
        private CultureInfo _currentLanguage;

        public CultureInfo CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                ApplyLanguageResources(_currentLanguage);
            }
        }

        public void AddLanguageResources(CultureInfo cultureInfo, Dictionary<string, string> languageResources)
        {
            _languageResources[cultureInfo.Name] = languageResources;
        }

        private void ApplyLanguageResources(CultureInfo cultureInfo)
        {
            if (!_languageResources.ContainsKey(cultureInfo.Name))
            {
                return;
            }

            foreach (var resources in _languageResources[cultureInfo.Name])
            {
                this[resources.Key] = resources.Value;
            }
        }

        public bool Remove(string key)
        {
            return _languageResources.Remove(key);
        }

        public override string GetDefault(string key)
        {
            return key;
        }
    }
}