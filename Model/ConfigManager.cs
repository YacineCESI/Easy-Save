using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Model
{

    public class ConfigManager
    {
        private string _configPath;
        private Dictionary<string, object> _settings;


        public ConfigManager()
        {

        }

        public bool SaveConfig(Dictionary<string, object> settings)
        { }


        public Dictionary<string, object> LoadConfig()
        { }


        public T GetSetting<T>(string key, T defaultValue = default)
        { }


        public bool SetSetting<T>(string key, T value)
        { }

        public string GetLanguage()
        { }

        public void SetLanguage(string language)
        { }

        public string GetConfigPath()
        { }

        public void SetConfigPath(string path)
        { }

        }
}