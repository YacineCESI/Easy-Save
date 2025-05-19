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
                        exit = ConfirmExit();
                        break;
                    default:
                        Console.WriteLine(("invalidOption"));
                        WaitForKey(); // Ajout d'un appel � une m�thode pour attendre une entr�e utilisateur
                        break; // Ajout d'une instruction break pour corriger l'erreur CS8070
           
                    

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
            Console.WriteLine("1. Create Job" );
            Console.WriteLine("2. Menu List Job" );
            Console.WriteLine("3. exit");
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
        { }

        private void RunAllBackupJobs()
        { }

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
