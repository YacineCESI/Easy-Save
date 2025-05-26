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

        public int MaxParallelJobs { get; private set; } = Environment.ProcessorCount; // Default to number of CPU cores

            public ConfigManager()
        {
            string baseConfigDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave");

            Directory.CreateDirectory(baseConfigDirectory);

            _configPath = Path.Combine(baseConfigDirectory, "config.json");
            _settings = new Dictionary<string, object>();

            if (File.Exists(_configPath))
            {
                LoadConfig();
            }
            else
            {
                _settings["Language"] = "en";
                _settings["MaxParallelJobs"] = MaxParallelJobs;
                SaveConfig(_settings);
            }
        }

        public bool SaveConfig(Dictionary<string, object> settings)
        {
            try
            {
                var configObj = new Dictionary<string, object>(settings)
                {
                    { "MaxParallelJobs", MaxParallelJobs }
                };

                string jsonString = JsonSerializer.Serialize(configObj, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_configPath, jsonString);
                _settings = settings;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, object> LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string jsonString = File.ReadAllText(_configPath);
                    var configData = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    if (configData.TryGetProperty("MaxParallelJobs", out JsonElement maxParallelJobsElement) &&
                        maxParallelJobsElement.TryGetInt32(out int maxParallelJobs))
                    {
                        MaxParallelJobs = maxParallelJobs > 0 ? maxParallelJobs : Environment.ProcessorCount;
                    }

                    _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString) ??
                        new Dictionary<string, object>();
                }
                return _settings;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement element)
                    {
                        return (T)Convert.ChangeType(element.GetRawText().Trim('"'), typeof(T));
                    }
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public bool SetSetting<T>(string key, T value)
        {
            try
            {
                _settings[key] = value;
                return SaveConfig(_settings);
            }
            catch
            {
                return false;
            }
        }

        public void SetMaxParallelJobs(int maxJobs)
        {
            if (maxJobs <= 0)
                maxJobs = Environment.ProcessorCount;

            MaxParallelJobs = maxJobs;
            SaveConfig(_settings);
        }

        public string GetLanguage()
        {
            return GetSetting<string>("Language", "en");
        }

        public void SetLanguage(string language)
        {
            SetSetting("Language", language);
        }

        public string GetConfigPath()
        {
            return GetSetting<string>("ConfigPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave"));
        }

        public void SetConfigPath(string path)
        {
            SetSetting("ConfigPath", path);
        }

        public List<string> GetExtensionsToEncrypt()
        {
            if (_settings.TryGetValue("ExtensionsToEncrypt", out var value) && value is List<string> list)
                return list;
            return new List<string>();
        }

        public void SetExtensionsToEncrypt(List<string> extensions)
        {
            _settings["ExtensionsToEncrypt"] = extensions;
        }

        public string GetCryptoSoftPath()
        {
            if (_settings.TryGetValue("CryptoSoftPath", out var value) && value is string path)
                return path;
            return string.Empty;
        }

        public void SetCryptoSoftPath(string path)
        {
            _settings["CryptoSoftPath"] = path;
        }
    }
}
