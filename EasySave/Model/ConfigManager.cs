using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace EasySave.Model
{

    public class ConfigManager : INotifyPropertyChanged
    {
        private string _configPath;
        private Dictionary<string, object> _settings;

        public int MaxParallelJobs { get; private set; } = Environment.ProcessorCount; // Default to number of CPU cores

        private List<string> _priorityExtensions = new List<string> { ".pdf" }; // Default, can be loaded from config

        private int _bandwidthLimitKB = 1024; // Default: 1MB

        public int BandwidthLimitKB
        {
            get => _bandwidthLimitKB;
            set
            {
                if (_bandwidthLimitKB != value)
                {
                    _bandwidthLimitKB = value;
                    SaveBandwidthLimit();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BandwidthLimitKB)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> PriorityExtensions { get; private set; } = new();

        public string PriorityExtensionsDisplay => string.Join(", ", PriorityExtensions);

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

            LoadPriorityExtensions();
            LoadBandwidthLimit();
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

        public List<string> GetPriorityExtensions()
        {
            // Always return a new list to avoid reference issues
            return PriorityExtensions.ToList();
        }

        public void SetPriorityExtensions(List<string> extensions)
        {
            if (extensions == null) extensions = new List<string>();
            _priorityExtensions = new List<string>(extensions);
            PriorityExtensions.Clear();
            foreach (var ext in _priorityExtensions)
                PriorityExtensions.Add(ext);

            // Save them
            SavePriorityExtensions();

            // Notify listeners (this is correct, as the property is named PriorityExtensions)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriorityExtensions)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriorityExtensionsDisplay)));
        }

        private void LoadPriorityExtensions()
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dict != null && dict.TryGetValue("PriorityExtensions", out var value) && value is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
                {
                    _priorityExtensions = elem.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
            }
            else
            {
                _priorityExtensions = new List<string> { ".pdf" };
            }
            PriorityExtensions.Clear();
            foreach (var ext in _priorityExtensions)
                PriorityExtensions.Add(ext);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriorityExtensions)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriorityExtensionsDisplay)));
        }

        private void SavePriorityExtensions()
        {
            string configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave");
            Directory.CreateDirectory(configDir);
            string configPath = Path.Combine(configDir, "config.json");
            Dictionary<string, object> dict = new();
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
            dict["PriorityExtensions"] = _priorityExtensions;
            File.WriteAllText(configPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void LoadBandwidthLimit()
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dict != null && dict.TryGetValue("BandwidthLimitKB", out var value))
                {
                    if (value is JsonElement elem && elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out int kb))
                        _bandwidthLimitKB = kb;
                    else if (value is int kb2)
                        _bandwidthLimitKB = kb2;
                }
            }
        }

        private void SaveBandwidthLimit()
        {
            string configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave");
            Directory.CreateDirectory(configDir);
            string configPath = Path.Combine(configDir, "config.json");
            Dictionary<string, object> dict = new();
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
            dict["BandwidthLimitKB"] = _bandwidthLimitKB;
            File.WriteAllText(configPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
