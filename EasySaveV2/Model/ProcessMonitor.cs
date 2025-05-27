using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Model
{
    public class ProcessMonitor
    {
        public event EventHandler<List<string>>? BlockedProcessDetected;

        private readonly List<string> _blockedProcesses;

        public ProcessMonitor()
        {
            _blockedProcesses = new List<string>();
        }

        public void AddBlockedProcess(string processName)
        {
            if (!string.IsNullOrEmpty(processName) && !_blockedProcesses.Contains(processName))
            {
                _blockedProcesses.Add(processName);
            }
        }

        public void RemoveBlockedProcess(string processName)
        {
            if (!string.IsNullOrEmpty(processName))
            {
                _blockedProcesses.Remove(processName);
            }
        }

        public bool IsProcessBlocked(string processName)
        {
            return !string.IsNullOrEmpty(processName) && _blockedProcesses.Contains(processName);
        }

        public List<string> GetRunningBlockedProcesses()
        {
            var runningBlockedProcesses = new List<string>();
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                try
                {
                    if (_blockedProcesses.Contains(process.ProcessName))
                    {
                        runningBlockedProcesses.Add(process.ProcessName);
                    }
                }
                catch (Exception)
                {
                    // Ignore processes that we can't access
                }
            }

            return runningBlockedProcesses;
        }

        public void CheckForBlockedProcesses()
        {
            var runningBlockedProcesses = GetRunningBlockedProcesses();
            if (runningBlockedProcesses.Any())
            {
                BlockedProcessDetected?.Invoke(this, runningBlockedProcesses);
            }
        }
    }
} 