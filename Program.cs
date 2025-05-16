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
            


            var mainViewModel = new MainViewModel();

            
            var consoleInterface = new ConsoleInterface(mainViewModel);

           
            consoleInterface.Start();
        }
    }
}