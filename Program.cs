using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.View;
using EasySave.ViewModel;

namespace EasySave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize the main view model1


            var mainViewModel = new MainViewModel();

            // Initialize the console interface
            var consoleInterface = new ConsoleInterface(mainViewModel);

            // Start the application
            consoleInterface.Start();
        }
    }
}