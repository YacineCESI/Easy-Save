using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy_Save;
using EasySave.View;
using EasySave.ViewModel;

namespace EasySave;

    class Program
    {
    [STAThread]

    static void Main(string[] args)
        {
            // Initialize the main view model1


            var mainViewModel = new MainViewModel();

            // Initialize the console interface
            var consoleInterface = new ConsoleInterface(mainViewModel);
            //var window = new NewWindow();
            //window.Show();

            // Start the application
            consoleInterface.Start();
        }
    }
