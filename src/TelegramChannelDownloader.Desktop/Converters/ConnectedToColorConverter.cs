using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts connection status to appropriate indicator colors
/// </summary>
public class ConnectedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Colors.Green : Colors.Red;
        }
        
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}