using System;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using EasySave.Model;
using EasySave.Model.Enums;
using EasySave.ViewModel;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Controls;

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for JobCreation.xaml
    /// </summary>
    public partial class JobCreation : Window
    {
        private BackupJobViewModel _viewModel;
        private readonly MainViewModel _mainViewModel;

        public JobCreation(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _viewModel = new BackupJobViewModel(); 
            InitializeComponent();
            InitializeViewModel();
        }

        /// <summary>
        /// Initializes the view model and sets up the UI
        /// </summary>
        private void InitializeViewModel()
        {
            // Only initialize the ViewModel, do NOT create a BackupJob instance here! (because it was initializing the BackupJob with default values empty so it was impossible to open the create job window)
            _viewModel = new BackupJobViewModel();

            // Set default log format
            _viewModel.LogFormat = LogFormat.JSON;
            
            if (_viewModel.ExtensionsToEncrypt == null)
                _viewModel.ExtensionsToEncrypt = new System.Collections.ObjectModel.ObservableCollection<string>();
            if (_viewModel.BlockedProcesses == null)
                _viewModel.BlockedProcesses = new System.Collections.ObjectModel.ObservableCollection<string>();

            DataContext = _viewModel;
        }

        /// <summary>
        /// Event handler for backup type checkbox selection
        /// </summary>
        private void BackupTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;
            
         
            if (checkbox.Tag.ToString() == "FULL")
            {
                _viewModel.Type = BackupType.FULL;
              
                if (DifferentialBackupCheckBox != null)
                    DifferentialBackupCheckBox.IsChecked = false;
            }
            else
            {
                _viewModel.Type = BackupType.DIFFERENTIAL;
            
                if (FullBackupCheckBox != null)
                    FullBackupCheckBox.IsChecked = false;
            }
        }

        private void EncryptFilesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                _viewModel.EncryptFiles = checkBox.IsChecked == true;
            }
        }

        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select a file in the source directory",
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                if (dialog.ShowDialog() == true)
                {
                    _viewModel.SourceDirectory = Path.GetDirectoryName(dialog.FileName);
                }
            }
        }

        private void BrowseTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a file in the target directory",
                CheckFileExists = true,
                CheckPathExists = true
            };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.TargetDirectory = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private bool CanSaveJob()
        {
            return !string.IsNullOrWhiteSpace(_viewModel.Name) &&
                   !string.IsNullOrWhiteSpace(_viewModel.SourceDirectory) &&
                   !string.IsNullOrWhiteSpace(_viewModel.TargetDirectory);
        }

        private void SaveJob()
        {
        
            MessageBox.Show("This is a view-only form. No job will be created.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void AddExtensionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ExtensionTextBox.Text))
            {
                string extension = ExtensionTextBox.Text.Trim();

              
                if (extension.StartsWith("."))
                {
                    extension = extension.Substring(1);
                }

                if (!_viewModel.ExtensionsToEncrypt.Contains(extension))
                {
                    _viewModel.ExtensionsToEncrypt.Add(extension);
                    ExtensionTextBox.Clear();
                }
            }
        }

        private void RemoveExtensionButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ExtensionsListBox.SelectedItems;
            if (selectedItems.Count > 0)
            {
                var itemsToRemove = new object[selectedItems.Count];
                selectedItems.CopyTo(itemsToRemove, 0);

                foreach (var item in itemsToRemove)
                {
                    _viewModel.ExtensionsToEncrypt.Remove(item.ToString());
                }
            }
        }

        private void ProcessTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
          
            AddProcessButton.IsEnabled = !string.IsNullOrWhiteSpace(ProcessTextBox.Text);
            
         
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkBrush.Color);
                textBox.ToolTip = null;
            }
        }

        private void AddProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ProcessTextBox.Text))
            {
                string process = ProcessTextBox.Text.Trim();

        
                if (process.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    process = process.Substring(0, process.Length - 4);
                }

           
                if (!IsValidProcessName(process))
                {
                    MessageBox.Show("Process name contains invalid characters. Please enter a valid process name.", 
                        "Invalid Process Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_viewModel.BlockedProcesses.Contains(process))
                {
                    _viewModel.BlockedProcesses.Add(process);
                    ProcessTextBox.Clear();
       
                    AddProcessButton.IsEnabled = false;
                }
            }
        }

        private bool IsValidProcessName(string processName)
        {
        
            return !string.IsNullOrEmpty(processName) && 
                   !processName.Contains('/') && !processName.Contains('\\') &&
                   !processName.Contains(':') && !processName.Contains('*') &&
                   !processName.Contains('?') && !processName.Contains('"') &&
                   !processName.Contains('<') && !processName.Contains('>') &&
                   !processName.Contains('|');
        }

        private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ProcessesListBox.SelectedItems;
            if (selectedItems.Count > 0)
            {
                var itemsToRemove = new object[selectedItems.Count];
                selectedItems.CopyTo(itemsToRemove, 0);

                foreach (var item in itemsToRemove)
                {
                    _viewModel.BlockedProcesses.Remove(item.ToString());
                }
                
                
                RemoveProcessButton.IsEnabled = ProcessesListBox.SelectedItems.Count > 0;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                MessageBox.Show("Job name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_viewModel.SourceDirectory))
            {
                MessageBox.Show("Source directory cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_viewModel.TargetDirectory))
            {
                MessageBox.Show("Target directory cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_viewModel.Type != BackupType.FULL && _viewModel.Type != BackupType.DIFFERENTIAL)
            {
                MessageBox.Show("Please select a backup type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

          
            bool success = _viewModel.CreateAndSaveBackupJob(_mainViewModel.BackupManager);

            if (success)
            {
                MessageBox.Show("Backup job created and saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("A job with the same name already exists or an error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkBrush.Color);
                textBox.ToolTip = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void LogFormatRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            
            var radioButton = sender as RadioButton;
            if (radioButton == null) return;
            
            if (radioButton.Name == "JsonFormatRadioButton")
            {
                _viewModel.LogFormat = LogFormat.JSON;
            }
            else if (radioButton.Name == "XamlFormatRadioButton")
            {
                _viewModel.LogFormat = LogFormat.XAML;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // No longer loading priority extensions from config
        }
    }
}
