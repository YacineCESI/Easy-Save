using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EasySave.Model;
using EasySave.Model.Enums;
using EasySave.ViewModel;

namespace EasySave.View
{
    public class ConsoleInterface
    {
        private readonly MainViewModel _viewModel;


        public ConsoleInterface(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }
        public void Start()
        {
          bool exit = false;

            while (!exit)
            {
                Console.Clear();
                DisplayHeader();
                DisplayMenu();

            
                string choice = GetUserInput();

                switch (choice)
                {
                    case "1":
                        CreateBackupJob();
                        break;
                    case "2":
                        DisplayBackupJobs();
                        break;
                    case "3":
                        RunBackupJob();
                        break;
                    case "4":
                        RunAllBackupJobs();
                        break;
                    case "5":
                        RemoveBackupJob();  // New option
                        break;
                    case "6":
                        exit = ConfirmExit();
                        break;
                       default:
                        Console.WriteLine(("invalidOption"));
                        WaitForKey(); 
                        break; 
           
                    

                }   

            }


        }

        private void DisplayHeader()
        {

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=====================================");
            Console.WriteLine("appTitle");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.ResetColor();
        }

        private void DisplayMenu()
        {
            Console.WriteLine("1. Create Job" );
            Console.WriteLine("2. Menu List Job" );
            Console.WriteLine("3. Run Job" );
            Console.WriteLine("4. Run All Jobs" );
            Console.WriteLine("5. menuRemoveJob");
            Console.WriteLine("6. exit");
        }

        private string GetUserInput()
        {
        return Console.ReadLine();
        }

        private void CreateBackupJob()
        { 
           Console.Clear();
           DisplayHeader();

            Console.WriteLine("Create Job");
            string jobName = GetUserInput();

            Console.WriteLine("enterSourceDirectory");
            string sourceDirectory = GetUserInput();

            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine("invalidSourceDirectory");
                WaitForKey();
                return;
            }

            Console.WriteLine("Target diriectory");
            string targetDirectory = GetUserInput();

            Console.WriteLine("Backup type \n1.full\n2.differential):");
            string backupTypeInput = GetUserInput();

          
                BackupType backupType = BackupType.FULL;
            
             
            if (backupTypeInput == "2")
            {
               backupType = BackupType.DIFFERENTIAL;
            }
           
            bool success = _viewModel.CreateBackupJob(jobName, sourceDirectory, targetDirectory, backupType);
            if (success)
            {
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("jo bCreated succeffuly");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("job Error");
                Console.ResetColor();
            }

            WaitForKey();

        }

        private void DisplayBackupJobs()
        {
            Console.Clear();
            DisplayHeader();

            List<BackupJob> jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine("No backup jobs found.");
                WaitForKey();
                return;
            }
            foreach (var job in jobs)
            {
                Console.WriteLine($"Job: {job.Name}, \nSource: {job.SourceDirectory}, \nTarget: {job.TargetDirectory}, \nState: {job.State}");
            }
            WaitForKey();
        }

        private void RunBackupJob()
        {
            Console.Clear();
            DisplayHeader();

            List<BackupJob> jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine("noJobs");
                WaitForKey();
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {jobs[i].Name} ({jobs[i].GetState()})");
            }

            Console.WriteLine();
            Console.WriteLine("selectJob");

            string input = GetUserInput();

            if (!int.TryParse(input, out int jobIndex) || jobIndex < 1 || jobIndex > jobs.Count)
            {
                Console.WriteLine("invalidJobNumber");
                WaitForKey();
                return;
            }

            BackupJob selectedJob = jobs[jobIndex - 1];

            Console.WriteLine("jobStarted");

            Thread monitorThread = new Thread(() =>
            {
                BackupJob job = selectedJob;
                string jobName = job.Name;
                bool running = true;

                while (running)
                {
                   
                    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

                    
                    float progress = job.GetProgress();
                    JobState state = job.GetState();

                    string progressBar = "[";
                    int barWidth = 50;
                    int filledWidth = (int)(progress / 100 * barWidth);

                    for (int i = 0; i < barWidth; i++)
                    {
                        progressBar += (i < filledWidth) ? "█" : "░";
                    }

                    progressBar += $"] {progress:F1}% - {state}";
                    Console.Write(progressBar);

              
                    if (state != JobState.RUNNING && state != JobState.PAUSED)
                    {
                        running = false;
                    }

                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);

                        if (key.Key == ConsoleKey.P)
                        {
                            if (state == JobState.RUNNING)
                            {
                               //_viewModel.PauseJob(jobName);
                                Console.WriteLine();
                                Console.WriteLine("jobPaused");
                                Console.ReadKey(true);
                                //_viewModel.ResumeJob(jobName);
                            }
                        }
                        else if (key.Key == ConsoleKey.S)
                        {
                            if (state == JobState.RUNNING || state == JobState.PAUSED)
                            {
                                //_viewModel.StopJob(jobName);
                                Console.WriteLine();
                                Console.WriteLine("jobStopped");
                                running = false;
                            }
                        }
                    }

                    Thread.Sleep(100);
                }

                Console.WriteLine();

                if (job.GetState() == JobState.COMPLETED)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("jobCompleted");
                    Console.ResetColor();
                }
                else if (job.GetState() == JobState.FAILED)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("jobFailed");
                    Console.ResetColor();
                }
            });

     
            bool success = _viewModel.ExecuteBackupJob(selectedJob.Name);

            monitorThread.Start();
            monitorThread.Join();

            WaitForKey();
        }

    
        private void RunAllBackupJobs()
        {
            Console.Clear();
            DisplayHeader();

            List<BackupJob> jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine("noJobs");
                WaitForKey();
                return;
            }

            Console.WriteLine("Running all jobs...");

            bool success = _viewModel.ExecuteAllBackupJobs();

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("allJobsCompleted");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("allJobsCompleted");
                Console.WriteLine("Some jobs may have failed. Check the logs.");
                Console.ResetColor();
            }

            WaitForKey();
        }


        private void RemoveBackupJob()
        {
            Console.Clear();
            DisplayHeader();

            List<BackupJob> jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine("noJobs");
                WaitForKey();
                return;
            }

            // Display all jobs
            for (int i = 0; i < jobs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {jobs[i].Name}");
            }

            Console.WriteLine();
            Console.WriteLine("Enter the number of the job to remove, or 0 to cancel:");

            string input = GetUserInput();

            // Check for cancel operation
            if (input == "0")
            {
                return;
            }

            // Validate input
            if (!int.TryParse(input, out int jobIndex) || jobIndex < 1 || jobIndex > jobs.Count)
            {
                Console.WriteLine("invalidJobNumber");
                WaitForKey();
                return;
            }

            // Get the job name
            string jobName = jobs[jobIndex - 1].Name;

            // Confirm deletion
            Console.WriteLine($"Are you sure you want to remove job '{jobName}'? (y/n)");
            string confirmation = GetUserInput().ToLower();

            if (confirmation == "y" || confirmation == "yes" || confirmation == "o" || confirmation == "oui")
            {
                // Remove the job
                bool success = _viewModel.RemoveJob(jobName);

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Job '{jobName}' was successfully removed.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error removing job.");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("Operation cancelled.");
            }

            WaitForKey();
        }

        private void ChangeSettings()
        { }

        private void WaitForKey()
        {
            Console.WriteLine();
            Console.WriteLine("pressAnyKey");
            Console.ReadKey(true);

        }

        private bool ConfirmExit()
        {
            Console.WriteLine("confirmExit");
            string input = GetUserInput().ToLower();

            return (input == "y" || input == "o" || input == "yes" || input == "oui");
        }

        }

}