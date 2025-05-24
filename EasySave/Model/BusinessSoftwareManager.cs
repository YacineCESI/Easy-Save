using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Model
{
    public class BusinessSoftwareManager
    {
        public bool IsBusinessSoftwareRunning(List<string> blockedProcesses)
        {
            if (blockedProcesses == null || blockedProcesses.Count == 0)
            {
                return false;
            }

            try
            {
                // Get all running processes
                Process[] runningProcesses = Process.GetProcesses();
                
                // Check if any blocked process is running
                foreach (string blockedProcess in blockedProcesses)
                {
                    if (string.IsNullOrWhiteSpace(blockedProcess))
                    {
                        continue;
                    }
                    
                    if (runningProcesses.Any(p => p.ProcessName.Equals(blockedProcess, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception)
            {
                // If there's an error accessing process info, be safe and assume blocked
                return true;
            }
        }
    }
}
