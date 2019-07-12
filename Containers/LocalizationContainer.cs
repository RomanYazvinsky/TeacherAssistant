using System.Collections.Generic;
using System.Globalization;

namespace TeacherAssistant.ComponentsImpl
{
    public class LocalizationContainer : SafeDictionary<string>
    {
        private readonly Dictionary<string, Dictionary<string, string>> _languageResources =
            new Dictionary<string, Dictionary<string, string>>();

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