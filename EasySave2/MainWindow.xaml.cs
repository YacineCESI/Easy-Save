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

namespace EasySave
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
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Création d'un nouveau job de sauvegarde");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouverture des paramètres");
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reprise du job sélectionné");
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pause du job sélectionné");
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Suppression du job sélectionné");
        }

        private void RunAllButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exécution de tous les jobs");
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Arrêt du job sélectionné");
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exécution du job sélectionné");
        }
    }
}