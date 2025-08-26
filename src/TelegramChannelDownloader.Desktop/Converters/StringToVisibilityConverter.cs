using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts string values to Visibility (empty/null = Collapsed, non-empty = Visible)
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }
        
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}