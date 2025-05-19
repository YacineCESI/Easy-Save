using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Easy_Save.View;

namespace EasySave;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var app = new Application();
        var mainWindow = new MainWindow();
        app.Run(mainWindow);
    }
}
