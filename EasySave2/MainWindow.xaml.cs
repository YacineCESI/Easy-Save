using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EasySave.ViewModel;
using EasySave.Model;

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            RefreshJobsList();
        }

        private void RefreshJobsList()
        {
            JobsListView.ItemsSource = _viewModel.GetAllJobs();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Ouvrir une fenêtre de dialogue pour créer un nouveau job
            MessageBox.Show("Création d'un nouveau job de sauvegarde");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouverture des paramètres");
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedJob = JobsListView.SelectedItem as BackupJob;
            if (selectedJob != null)
            {
                _viewModel.ResumeJob(selectedJob.Name);
                RefreshJobsList();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un job à reprendre");
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedJob = JobsListView.SelectedItem as BackupJob;
            if (selectedJob != null)
            {
                _viewModel.PauseJob(selectedJob.Name);
                RefreshJobsList();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un job à mettre en pause");
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedJob = JobsListView.SelectedItem as BackupJob;
            if (selectedJob != null)
            {
                if (MessageBox.Show($"Voulez-vous vraiment supprimer le job {selectedJob.Name} ?", 
                    "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _viewModel.RemoveJob(selectedJob.Name);
                    RefreshJobsList();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un job à supprimer");
            }
        }

        private void RunAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExecuteAllBackupJobs())
            {
                MessageBox.Show("Tous les jobs ont été lancés");
                RefreshJobsList();
            }
            else
            {
                MessageBox.Show("Erreur lors du lancement des jobs");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedJob = JobsListView.SelectedItem as BackupJob;
            if (selectedJob != null)
            {
                _viewModel.StopJob(selectedJob.Name);
                RefreshJobsList();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un job à arrêter");
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedJob = JobsListView.SelectedItem as BackupJob;
            if (selectedJob != null)
            {
                if (_viewModel.ExecuteBackupJob(selectedJob.Name))
                {
                    MessageBox.Show($"Le job {selectedJob.Name} a été lancé");
                    RefreshJobsList();
                }
                else
                {
                    MessageBox.Show($"Erreur lors du lancement du job {selectedJob.Name}");
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un job à exécuter");
            }
        }
    }
}