using System;
using System.Globalization;
using System.Windows.Data;

namespace TelegramChannelDownloader.Converters;

/// <summary>
/// Converts TimeSpan to user-friendly string representation
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
            return "Unknown";

        if (timeSpan == TimeSpan.Zero)
            return "Calculating...";

        if (timeSpan.TotalSeconds < 1)
            return "Less than 1 second";

        if (timeSpan.TotalMinutes < 1)
            return $"{(int)timeSpan.TotalSeconds} second{(timeSpan.TotalSeconds >= 2 ? "s" : "")}";

        if (timeSpan.TotalHours < 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            var seconds = timeSpan.Seconds;
            return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
        }

        if (timeSpan.TotalDays < 1)
        {
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        var days = (int)timeSpan.TotalDays;
        var remainingHours = timeSpan.Hours;
        return remainingHours > 0 ? $"{days}d {remainingHours}h" : $"{days}d";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("TimeSpanToStringConverter does not support ConvertBack");
    }
}