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
                        RemoveBackupJob(); 
                        break;
                    case "6":
                        ChangeSettings();   
                        break;
                    case "7":
                        exit = ConfirmExit(); 
                        break;
                    default:
                        Console.WriteLine(_viewModel.GetString("invalidOption"));
                        WaitForKey();
                        break;
                }

            }
        }

  
        private void DisplayHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=====================================");
            Console.WriteLine(_viewModel.GetString("appTitle"));
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.ResetColor();
        }

    

        private void DisplayMenu()
        {
            Console.WriteLine("1. " + _viewModel.GetString("menuCreateJob"));
            Console.WriteLine("2. " + _viewModel.GetString("menuListJobs"));
            Console.WriteLine("3. " + _viewModel.GetString("menuRunJob"));
            Console.WriteLine("4. " + _viewModel.GetString("menuRunAllJobs"));
            Console.WriteLine("5. " + _viewModel.GetString("menuRemoveJob"));  // New option
            Console.WriteLine("6. " + _viewModel.GetString("menuSettings"));   // Changed from 5 to 6
            Console.WriteLine("7. " + _viewModel.GetString("menuExit"));       // Changed from 6 to 7
            Console.WriteLine();
            Console.Write("> ");
        }


        private string GetUserInput()
        {
            return Console.ReadLine();
        }


        private void CreateBackupJob()
        {
            Console.Clear();
            DisplayHeader();

            Console.WriteLine(_viewModel.GetString("promptJobName"));
            string jobName = GetUserInput();

            Console.WriteLine(_viewModel.GetString("promptSourceDir"));
            string sourceDir = GetUserInput();

     
            if (!Directory.Exists(sourceDir))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(_viewModel.GetString("dirNotFound"));
                Console.ResetColor();
                WaitForKey();
                return;
            }

            Console.WriteLine(_viewModel.GetString("promptTargetDir"));
            string targetDir = GetUserInput();

            Console.WriteLine(_viewModel.GetString("promptBackupType"));
            string typeInput = GetUserInput();

            BackupType type = BackupType.FULL;
            if (typeInput == "2")
            {
                type = BackupType.DIFFERENTIAL;
            }

            bool success = _viewModel.CreateBackupJob(jobName, sourceDir, targetDir, type);

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_viewModel.GetString("jobCreated"));
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(_viewModel.GetString("jobCreateError"));
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
                Console.WriteLine(_viewModel.GetString("noJobs"));
                WaitForKey();
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                Console.WriteLine($"{i + 1}. {job.Name}");
                Console.WriteLine($"   {_viewModel.GetString("promptSourceDir")} {job.SourceDirectory}");
                Console.WriteLine($"   {_viewModel.GetString("promptTargetDir")} {job.TargetDirectory}");
                Console.WriteLine($"   {_viewModel.GetString("promptBackupType")} {job.Type}");
                Console.WriteLine($"   State: {job.GetState()}");
                Console.WriteLine($"   Progress: {job.GetProgress():F1}%");
                Console.WriteLine();
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
                Console.WriteLine(_viewModel.GetString("noJobs"));
                WaitForKey();
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {jobs[i].Name} ({jobs[i].GetState()})");
            }

            Console.WriteLine();
            Console.WriteLine(_viewModel.GetString("selectJob"));

            string input = GetUserInput();

            if (!int.TryParse(input, out int jobIndex) || jobIndex < 1 || jobIndex > jobs.Count)
            {
                Console.WriteLine(_viewModel.GetString("invalidJobNumber"));
                WaitForKey();
                return;
            }

      
            BackupJob selectedJob = jobs[jobIndex - 1];

            Console.WriteLine(_viewModel.GetString("jobStarted"));


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
                                _viewModel.PauseJob(jobName);
                                Console.WriteLine();
                                Console.WriteLine(_viewModel.GetString("jobPaused"));
                                Console.ReadKey(true);
                                _viewModel.ResumeJob(jobName);
                            }
                        }
                        else if (key.Key == ConsoleKey.S)
                        {
                            if (state == JobState.RUNNING || state == JobState.PAUSED)
                            {
                                _viewModel.StopJob(jobName);
                                Console.WriteLine();
                                Console.WriteLine(_viewModel.GetString("jobStopped"));
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
                    Console.WriteLine(_viewModel.GetString("jobCompleted"));
                    Console.ResetColor();
                }
                else if (job.GetState() == JobState.FAILED)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(_viewModel.GetString("jobFailed"));
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
                Console.WriteLine(_viewModel.GetString("noJobs"));
                WaitForKey();
                return;
            }

            Console.WriteLine("Running all jobs...");

           
            bool success = _viewModel.ExecuteAllBackupJobs();

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_viewModel.GetString("allJobsCompleted"));
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(_viewModel.GetString("allJobsCompleted"));
                Console.WriteLine("Some jobs may have failed. Check the logs.");
                Console.ResetColor();
            }

            WaitForKey();
        }

    
        private void ChangeSettings()
        {
            Console.Clear();
            DisplayHeader();

            Console.WriteLine(_viewModel.GetString("changeLanguage"));
            string language = GetUserInput().ToLower();

            if (language == "en" || language == "fr")
            {
                bool success = _viewModel.ChangeLanguage(language);

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(_viewModel.GetString("languageChanged"));
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(_viewModel.GetString("invalidOption"));
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(_viewModel.GetString("invalidOption"));
                Console.ResetColor();
            }

            WaitForKey();
        }

    
        private void WaitForKey()
        {
            Console.WriteLine();
            Console.WriteLine(_viewModel.GetString("pressAnyKey"));
            Console.ReadKey(true);
        }

   
        private bool ConfirmExit()
        {
            Console.WriteLine(_viewModel.GetString("confirmExit"));
            string input = GetUserInput().ToLower();

            return (input == "y" || input == "o" || input == "yes" || input == "oui");
        }

        private void RemoveBackupJob()
        {
            Console.Clear();
            DisplayHeader();

            List<BackupJob> jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_viewModel.GetString("noJobs"));
                WaitForKey();
                return;
            }

   
            for (int i = 0; i < jobs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {jobs[i].Name}");
            }

            Console.WriteLine();
            Console.WriteLine("Enter the number of the job to remove, or 0 to cancel:");

            string input = GetUserInput();

            if (input == "0")
            {
                return;
            }

      
            if (!int.TryParse(input, out int jobIndex) || jobIndex < 1 || jobIndex > jobs.Count)
            {
                Console.WriteLine(_viewModel.GetString("invalidJobNumber"));
                WaitForKey();
                return;
            }

        
            string jobName = jobs[jobIndex - 1].Name;

   
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
    }
}
