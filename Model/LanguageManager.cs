using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace EasySave.Model
{
    /// <summary>
    /// Manages language translations for the application
    /// </summary>
    public class LanguageManager
    {
        private string _currentLanguage;
        private ConfigManager _configManager;
        private ResourceManager _resourceManager;
        private List<string> _availableLanguages;

        private static readonly ResourceManager ResourceManager =
            new ResourceManager("Easy_Save.Resources.Strings", typeof(LanguageManager).Assembly);

        public LanguageManager()
        {
            _configManager = new ConfigManager();

            _resourceManager = Resources.Strings.ResourceManager;


            LoadLanguages();


            _currentLanguage = _configManager.GetLanguage();


            if (!_availableLanguages.Contains(_currentLanguage))
            {
                _currentLanguage = "en";
                _configManager.SetLanguage(_currentLanguage);
            }


            SetCurrentCulture(_currentLanguage);
        }


        public string GetString(string key)
        {
            string result = _resourceManager.GetString(key);

            if (string.IsNullOrEmpty(result))
            {

                var originalCulture = Thread.CurrentThread.CurrentUICulture;
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                result = _resourceManager.GetString(key);
                Thread.CurrentThread.CurrentUICulture = originalCulture;


                if (string.IsNullOrEmpty(result))
                {
                    return key;
                }
            }

            return result;
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }


        public List<string> GetAvailableLanguages()
        {
            return _availableLanguages;
        }


        public bool SwitchLanguage(string language)
        {
            if (_availableLanguages.Contains(language))
            {
                _currentLanguage = language;
                _configManager.SetLanguage(language);
                SetCurrentCulture(language);
                return true;
            }
            return false;
        }


        private void SetCurrentCulture(string languageCode)
        {
            try
            {

                CultureInfo culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {

                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            }
        }


        public void LoadLanguages()
        {
            _availableLanguages = new List<string>();

            _availableLanguages.Add("en");

            if (_resourceManager.GetResourceSet(new CultureInfo("fr"), true, false) != null)
            {
                _availableLanguages.Add("fr");
            }

        }
    }
}