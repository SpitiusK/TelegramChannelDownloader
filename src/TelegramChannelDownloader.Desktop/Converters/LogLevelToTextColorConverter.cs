using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TelegramChannelDownloader.Desktop.ViewModels;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts LogLevel enum values to appropriate text colors for display
/// </summary>
public class LogLevelToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => new SolidColorBrush(Color.FromRgb(128, 128, 128)),     // Gray for trace
                LogLevel.Debug => new SolidColorBrush(Color.FromRgb(0, 128, 0)),        // Green for debug
                LogLevel.Info => new SolidColorBrush(Color.FromRgb(44, 62, 80)),        // Dark gray for info
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 140, 0)),    // Orange for warning
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(220, 20, 60)),      // Crimson for error
                _ => new SolidColorBrush(Color.FromRgb(44, 62, 80))                     // Default dark gray
            };
        }
        
        return new SolidColorBrush(Color.FromRgb(44, 62, 80)); // Default dark gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}