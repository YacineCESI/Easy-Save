using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Model
{
    public class BusinessSoftwareManager
    {
        /// <summary>
        /// Checks if any of the specified blocked processes are currently running
        /// </summary>
        /// <param name="blockedProcesses">List of process names to check</param>
        /// <returns>True if any blocked process is running, false otherwise</returns>
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

        /// <summary>
        /// Gets a list of blocked processes that are currently running
        /// </summary>
        /// <param name="blockedProcesses">List of process names to check</param>
        /// <returns>List of running blocked processes, empty list if none are running</returns>
        public List<string> GetRunningBlockedProcesses(List<string> blockedProcesses)
        {
            List<string> runningBlockedProcesses = new List<string>();
            
            if (blockedProcesses == null || blockedProcesses.Count == 0)
            {
                return runningBlockedProcesses;
            }

            try
            {
                // Get all running processes
                Process[] runningProcesses = Process.GetProcesses();
                
                // Check each blocked process
                foreach (string blockedProcess in blockedProcesses)
                {
                    if (string.IsNullOrWhiteSpace(blockedProcess))
                    {
                        continue;
                    }
                    
                    if (runningProcesses.Any(p => p.ProcessName.Equals(blockedProcess, StringComparison.OrdinalIgnoreCase)))
                    {
                        runningBlockedProcesses.Add(blockedProcess);
                    }
                }
            }
            catch (Exception)
            {
                // If there's an error, return empty list
            }
            
            return runningBlockedProcesses;
        }
    }
}
