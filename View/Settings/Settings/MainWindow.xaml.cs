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

namespace Settings
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

        private void Change_Language_Settings(object sender, RoutedEventArgs e)
        {
            // Assume sender is a ComboBox with language options: "English" and "French"
            if (sender is ComboBox comboBox)
            {
                string selectedLanguage = comboBox.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedLanguage))
                    return;

                // Set culture based on selection
                var culture = selectedLanguage == "French" ? "fr-FR" : "en-US";
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(culture);
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);

                // Reload resources for the new culture
                var dict = new ResourceDictionary();
                switch (culture)
                {
                    case "fr-FR":
                        dict.Source = new System.Uri("..\\Resources\\StringResources.fr-FR.xaml", System.UriKind.Relative);
                        break;
                    default:
                        dict.Source = new System.Uri("..\\Resources\\StringResources.en-US.xaml", System.UriKind.Relative);
                        break;
                }

                // Remove previous language dictionaries
                var oldDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("StringResources."));
                if (oldDict != null)
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);

                Application.Current.Resources.MergedDictionaries.Add(dict);

                // Optionally, force UI to refresh
                foreach (Window window in Application.Current.Windows)
                {
                    var oldContent = window.Content;
                    window.Content = null;
                    window.Content = oldContent;
                }
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Fix: Use the Close() method from the base Window class
            base.Close();
        }
    }
}