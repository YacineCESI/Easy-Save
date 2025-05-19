using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Model;

namespace EasySave.ViewModel
{
    public class SettingsViewModel
    {
        private ConfigManager configManager;
        private CryptoSoftManager cryptoSoftManager;

        public ObservableCollection<string> Languages { get; set; }
        public string SelectedLanguage { get; set; }
        public ObservableCollection<string> BlockedProcesses { get; set; }
        public string ProcessPath { get; set; }
        public ObservableCollection<string> ExtensionsToEncrypt { get; set; }
        public string CryptoSoftPath { get; set; }

        public ICommand SaveSettingsCommand { get; set; }
        public ICommand ChangeLanguageCommand { get; set; }
        public ICommand AddBlockedProcessCommand { get; set; }
        public ICommand RemoveBlockedProcessCommand { get; set; }
        public ICommand AddExtensionToEncryptCommand { get; set; }
        public ICommand RemoveExtensionToEncryptCommand { get; set; }
        public ICommand BrowseCryptoSoftPathCommand { get; set; }
        public ICommand TestCryptoSoftCommand { get; set; }

        // ...constructor, command implementations, etc...
    }
}
