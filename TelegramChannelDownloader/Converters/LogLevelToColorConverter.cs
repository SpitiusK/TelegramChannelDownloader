using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TelegramChannelDownloader.Models;

namespace TelegramChannelDownloader.Converters;

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
                LogLevel.Info => new SolidColorBrush(Colors.LightGray),
                LogLevel.Warning => new SolidColorBrush(Colors.Orange),
                LogLevel.Error => new SolidColorBrush(Colors.LightCoral),
                _ => new SolidColorBrush(Colors.White)
            };
        }
        
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}