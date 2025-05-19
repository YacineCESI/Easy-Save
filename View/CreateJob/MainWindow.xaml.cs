using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CreateJob
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SourcePackageButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the folder of the currently running executable (assumed to be the solution's output folder)
            string folderPath = System.AppDomain.CurrentDomain.BaseDirectory;

            // Open the folder in File Explorer
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }

        private void DestinationPackageButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the folder of the currently running executable (assumed to be the solution's output folder)
            string folderPath = System.AppDomain.CurrentDomain.BaseDirectory;

            // Open the folder in File Explorer
            System.Diagnostics.Process.Start("explorer.exe", folderPath);

        }

        private void CreateJobButton_Click(object sender, RoutedEventArgs e)
        {
            // Pseudocode:
            // 1. Gather all relevant information from the Window's controls (e.g., TextBoxes, ComboBoxes, etc.)
            // 2. Build a string with all the information and the current datetime.
            // 3. Choose a file path (e.g., in the output folder, with a unique name).
            // 4. Write the string to the file.

            StringBuilder sb = new StringBuilder();

            // Example: Replace these with your actual control names and data extraction
            // sb.AppendLine($"Job Name: {JobNameTextBox.Text}");
            // sb.AppendLine($"Source Package: {SourcePackageTextBox.Text}");
            // sb.AppendLine($"Destination Package: {DestinationPackageTextBox.Text}");
            // sb.AppendLine($"Other Info: {OtherInfoTextBox.Text}");

            // For demonstration, we'll just add a placeholder
            sb.AppendLine("Job Information:");
            sb.AppendLine("Replace this with actual data from your controls.");

            sb.AppendLine($"Created At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            string folderPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string fileName = $"Job_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = System.IO.Path.Combine(folderPath, fileName);

            System.IO.File.WriteAllText(filePath, sb.ToString());

            MessageBox.Show($"Job file created:\n{filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Fix: Use the Close() method from the base Window class
            base.Close();
        }
    }
}