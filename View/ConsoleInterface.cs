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
                        WaitForKey(); // Ajout d'un appel à une méthode pour attendre une entrée utilisateur
                        break; // Ajout d'une instruction break pour corriger l'erreur CS8070
           
                    

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