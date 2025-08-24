using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TelegramChannelDownloader.Converters;

/// <summary>
/// Converts progress percentage to appropriate color brush
/// </summary>
public class ProgressToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int progress)
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4)); // Default blue

        // Green for completion, blue for active progress, gray for not started
        if (progress >= 100)
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0F, 0x7B, 0x0F)); // Success green
        
        if (progress > 0)
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4)); // Active blue
        
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC)); // Inactive gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ProgressToColorConverter does not support ConvertBack");
    }
}