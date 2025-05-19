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

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for CreateBackupJob.xaml
    /// </summary>
    public partial class CreateBackupJob : Window
    {
        public CreateBackupJob()
        {
            InitializeComponent();
        }

        private void SourcePackageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog(); // No need for System.Windows prefix
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Use System.Windows.Forms.DialogResult
            {
                // Assuming you have a TextBox named SourcePathTextBox in your XAML to display the selected path
                SourcePathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void DestinationPackageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog(); // No need for System.Windows prefix
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Use System.Windows.Forms.DialogResult
            {
                // Assuming you have a TextBox named SourcePathTextBox in your XAML to display the selected path
                SourcePathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CreateJobButton_Click(object sender, RoutedEventArgs e)
        {
            // Pseudocode:
            // 1. Retrieve values from UI controls (e.g., TextBoxes for source, destination, job name, etc.)
            // 2. Validate the input (ensure required fields are not empty)
            // 3. Create a string or object representing the backup job
            // 4. Write the information to a file (e.g., as JSON or plain text)
            // 5. Optionally, show a success or error message

            // 1. Retrieve values
            string sourcePath = SourcePathTextBox.Text;
            string destinationPath = DestinationPathTextBox.Text;
            string jobName = JobNameTextBox.Text;

            // 2. Validate input
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                string.IsNullOrWhiteSpace(destinationPath) ||
                string.IsNullOrWhiteSpace(jobName))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Create job info (as JSON)
            var jobInfo = new
            {
                Name = jobName,
                Source = sourcePath,
                Destination = destinationPath,
                CreatedAt = DateTime.Now
            };
            string json = System.Text.Json.JsonSerializer.Serialize(jobInfo, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
    }
}
