using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Collections.Concurrent;

namespace EasySave.Model
{
    public class BlockedProcessMonitor : IDisposable
    {
        private readonly BusinessSoftwareManager _businessSoftwareManager;
        private System.Timers.Timer _monitorTimer;
        private readonly int _checkIntervalMs = 1000; // Check every second

        public bool IsBlockedProcessRunning { get; private set; }
        public List<string> RunningBlockedProcesses { get; private set; }

        // Event to notify subscribers when blocked process state changes
        public event EventHandler<BlockedProcessEventArgs> BlockedProcessStateChanged;

        // Keep track of registered jobs to manage
        private ConcurrentDictionary<string, List<string>> _registeredJobs;

        public BlockedProcessMonitor(BusinessSoftwareManager businessSoftwareManager)
        {
            _businessSoftwareManager = businessSoftwareManager ?? throw new ArgumentNullException(nameof(businessSoftwareManager));
            RunningBlockedProcesses = new List<string>();
            _registeredJobs = new ConcurrentDictionary<string, List<string>>();

            // Set up timer for periodic checks
            _monitorTimer = new System.Timers.Timer(_checkIntervalMs);
            _monitorTimer.Elapsed += CheckBlockedProcesses;
        }

        public void RegisterJob(string jobName, List<string> blockedProcesses)
        {
            if (string.IsNullOrEmpty(jobName) || blockedProcesses == null)
                return;

            _registeredJobs[jobName] = blockedProcesses;

            // Start monitoring if this is the first job
            if (_registeredJobs.Count == 1)
            {
                Start();
            }
        }

        public void UnregisterJob(string jobName)
        {
            _registeredJobs.TryRemove(jobName, out _);

            // Stop monitoring if no more jobs
            if (_registeredJobs.Count == 0)
            {
                Stop();
            }
        }

        public void Start()
        {
            // Initial check before starting the timer
            PerformBlockedProcessCheck();

            _monitorTimer.Start();
        }

        public void Stop()
        {
            _monitorTimer.Stop();
        }

        private void CheckBlockedProcesses(object sender, ElapsedEventArgs e)
        {
            PerformBlockedProcessCheck();
        }

        private void PerformBlockedProcessCheck()
        {
            // Collect all unique blocked processes across all jobs
            HashSet<string> allBlockedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var processes in _registeredJobs.Values)
            {
                foreach (var process in processes)
                {
                    if (!string.IsNullOrWhiteSpace(process))
                    {
                        allBlockedProcesses.Add(process);
                    }
                }
            }

            // Check if any of the processes are running
            List<string> currentRunningProcesses = _businessSoftwareManager.GetRunningBlockedProcesses(allBlockedProcesses.ToList());
            bool wasRunning = IsBlockedProcessRunning;
            IsBlockedProcessRunning = currentRunningProcesses.Count > 0;

            // If state changed, trigger event
            if (wasRunning != IsBlockedProcessRunning ||
                !AreListsEqual(RunningBlockedProcesses, currentRunningProcesses))
            {
                RunningBlockedProcesses = currentRunningProcesses;

                BlockedProcessStateChanged?.Invoke(this, new BlockedProcessEventArgs
                {
                    IsBlocked = IsBlockedProcessRunning,
                    RunningProcesses = new List<string>(currentRunningProcesses)
                });
            }
        }

        private bool AreListsEqual(List<string> list1, List<string> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!list1[i].Equals(list2[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
        }
    }
    
public class BlockedProcessEventArgs : EventArgs
    {
        public bool IsBlocked { get; set; }
        public List<string> RunningProcesses { get; set; }
    }
}
