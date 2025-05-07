using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Model
{

    public class LanguageManager
    {
        private string _currentLanguage;
        private Dictionary<string, Dictionary<string, string>> _translations;
        private ConfigManager _configManager;

        public LanguageManager()
        { }

        public string GetString(string key)
        { }

        public string GetCurrentLanguage()
        { }

        public List<string> GetAvailableLanguages()
        { }

        public bool SwitchLanguage(string language)
        { }

        public void LoadLanguages()
        { }


        }
}
