using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySaveV2.Model
{
    public class ConfigManager
    {
        private readonly string _configPath;
        private ConfigData _config;

        public ConfigManager()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
            }
            else
            {
                _config = new ConfigData();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        public int GetMaxParallelJobs() => _config.MaxParallelJobs;
        public void SetMaxParallelJobs(int value)
        {
            _config.MaxParallelJobs = value;
            SaveConfig();
        }

        public int GetBandwidthLimit() => _config.BandwidthLimit;
        public void SetBandwidthLimit(int value)
        {
            _config.BandwidthLimit = value;
            SaveConfig();
        }

        public int GetNetworkLoadThreshold() => _config.NetworkLoadThreshold;
        public void SetNetworkLoadThreshold(int value)
        {
            _config.NetworkLoadThreshold = value;
            SaveConfig();
        }

        public List<string> GetPriorityExtensions() => _config.PriorityExtensions;
        public void SetPriorityExtensions(List<string> value)
        {
            _config.PriorityExtensions = value;
            SaveConfig();
        }

        public List<string> GetBlockedProcesses() => _config.BlockedProcesses;
        public void SetBlockedProcesses(List<string> value)
        {
            _config.BlockedProcesses = value;
            SaveConfig();
        }

        private class ConfigData
        {
            public int MaxParallelJobs { get; set; } = 2;
            public int BandwidthLimit { get; set; } = 1024 * 1024; // 1 MB
            public int NetworkLoadThreshold { get; set; } = 80; // 80%
            public List<string> PriorityExtensions { get; set; } = new List<string> { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };
            public List<string> BlockedProcesses { get; set; } = new List<string>();
        }
    }
} 