using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts null values to Visibility (null = Collapsed, non-null = Visible)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}