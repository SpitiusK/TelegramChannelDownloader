using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TelegramChannelDownloader.Desktop.Converters;

/// <summary>
/// Converts boolean validation status to appropriate border brush for input fields
/// </summary>
public class ValidationToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return isValid ? 
                new SolidColorBrush(Colors.Green) : 
                new SolidColorBrush(Colors.Red);
        }
        
        // Default border color (no validation state)
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}