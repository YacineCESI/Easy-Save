using System;
using System.Globalization;
using System.Windows.Data;

namespace EasySaveV2.Converters
{
    public class StringLocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EasySave.ViewModel.MainViewModel viewModel && parameter is string key)
            {
                return viewModel.GetLocalizedString(key);
            }
            return parameter?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
