using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EasySave.ViewModel;
using EasySave.Model.Enums;

namespace Easy_Save.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Ouvrir une fenêtre de dialogue pour créer un nouveau job
            MessageBox.Show("Création d'un nouveau job");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // TODO: Implémenter l'ouverture de la fenêtre des paramètres
            MessageBox.Show("Paramètres");
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implémenter la reprise du job sélectionné
            MessageBox.Show("Reprise du job");
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implémenter la pause du job sélectionné
            MessageBox.Show("Pause du job");
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implémenter la suppression du job sélectionné
            MessageBox.Show("Suppression du job");
        }

        private void RunAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExecuteAllBackupJobs())
            {
                MessageBox.Show("Tous les jobs ont été exécutés avec succès");
            }
            else
            {
                MessageBox.Show("Erreur lors de l'exécution des jobs");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implémenter l'arrêt du job sélectionné
            MessageBox.Show("Arrêt du job");
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implémenter l'exécution du job sélectionné
            MessageBox.Show("Exécution du job");
        }
    }
}
