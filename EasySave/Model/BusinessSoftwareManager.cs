using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Model
{
    public class BusinessSoftwareManager
    {
        private List<string> blockedProcesses = new();

        public bool IsBusinessSoftwareRunning()
        {
            var running = Process.GetProcesses().Select(p => p.ProcessName.ToLowerInvariant());
            return blockedProcesses.Any(bp => running.Contains(bp.ToLowerInvariant()));
        }

        public List<string> GetBlockedProcesses() => blockedProcesses;

        public void SetBlockedProcesses(List<string> processes)
        {
            blockedProcesses = processes;
        }
    }
}
