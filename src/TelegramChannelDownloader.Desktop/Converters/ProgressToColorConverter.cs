using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts progress percentage to appropriate colors for progress visualization
/// </summary>
public class ProgressToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int progress)
        {
            return progress switch
            {
                < 25 => new SolidColorBrush(Colors.Red),
                < 50 => new SolidColorBrush(Colors.Orange),
                < 75 => new SolidColorBrush(Colors.Yellow),
                < 100 => new SolidColorBrush(Colors.LightGreen),
                100 => new SolidColorBrush(Colors.Green),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}