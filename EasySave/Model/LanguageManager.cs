using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace EasySave.Model
{
    public class LanguageManager
    {
        private string _currentLanguage;
        private readonly ResourceManager _resourceManager;
        private readonly List<string> _availableLanguages;

        public LanguageManager()
        {
            // Initialize with the resource manager from the application's resources
            _resourceManager = new ResourceManager("EasySave.Resources.Strings", typeof(LanguageManager).Assembly);
            
            // Default language is English
            _currentLanguage = "en";
            
            // Define available languages
            _availableLanguages = new List<string> { "en", "fr", "es" };
            
            // Try to use system language if available
            string systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (_availableLanguages.Contains(systemLanguage))
            {
                _currentLanguage = systemLanguage;
            }
        }

        public string GetString(string key)
        {
            try
            {
                var culture = new CultureInfo(_currentLanguage);
                string value = _resourceManager.GetString(key, culture);
                return string.IsNullOrEmpty(value) ? key : value;
            }
            catch (Exception)
            {
                return key; // Return the key if resource not found
            }
        }

        public bool SwitchLanguage(string language)
        {
            if (_availableLanguages.Contains(language.ToLowerInvariant()))
            {
                _currentLanguage = language.ToLowerInvariant();
                return true;
            }
            return false;
        }

        public List<string> GetAvailableLanguages()
        {
            return _availableLanguages.ToList();
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
    }
}
