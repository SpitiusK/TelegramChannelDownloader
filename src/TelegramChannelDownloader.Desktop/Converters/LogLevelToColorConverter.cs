using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TelegramChannelDownloader.Desktop.ViewModels;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts LogLevel enum values to appropriate colors for display
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => new SolidColorBrush(Color.FromRgb(240, 248, 255)),      // AliceBlue - light blue
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 248, 220)),   // Cornsilk - light yellow 
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(255, 240, 245)),     // LavenderBlush - light pink
                _ => new SolidColorBrush(Color.FromRgb(248, 249, 250))                   // Very light gray
            };
        }
        
        return new SolidColorBrush(Color.FromRgb(248, 249, 250)); // Very light gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}