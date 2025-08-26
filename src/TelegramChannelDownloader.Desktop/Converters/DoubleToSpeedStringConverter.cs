using System.Globalization;
using System.Windows.Data;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts double speed values to readable string format
/// </summary>
public class DoubleToSpeedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double speed)
        {
            return speed >= 1.0 ? $"{speed:F1} msg/sec" : $"{speed:F2} msg/sec";
        }
        
        return "0.0 msg/sec";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}