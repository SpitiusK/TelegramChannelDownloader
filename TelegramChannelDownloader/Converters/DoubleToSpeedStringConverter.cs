using System;
using System.Globalization;
using System.Windows.Data;

namespace TelegramChannelDownloader.Converters;

/// <summary>
/// Converts double values to speed string (messages per second)
/// </summary>
public class DoubleToSpeedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double speed)
            return "0.0 msg/s";

        if (speed < 0.01)
            return "0.0 msg/s";

        if (speed < 1)
            return $"{speed:F2} msg/s";

        if (speed < 10)
            return $"{speed:F1} msg/s";

        return $"{speed:F0} msg/s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("DoubleToSpeedStringConverter does not support ConvertBack");
    }
}