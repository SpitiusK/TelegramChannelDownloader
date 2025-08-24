using System;
using System.Globalization;
using System.Windows.Data;
using TelegramChannelDownloader.Models;

namespace TelegramChannelDownloader.Converters;

/// <summary>
/// Converts MessageData to a preview string for display during download
/// </summary>
public class MessageDataToPreviewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MessageData message)
            return "Processing...";

        var preview = string.Empty;
        
        // Add message type indicator
        var typeIndicator = message.MessageType switch
        {
            MessageType.Text => "💬",
            MessageType.Photo => "📷",
            MessageType.Video => "🎥",
            MessageType.Audio => "🎵",
            MessageType.Document => "📄",
            MessageType.Animation => "🎬",
            MessageType.Voice => "🎤",
            MessageType.VideoNote => "📹",
            MessageType.Sticker => "🎭",
            MessageType.Location => "📍",
            MessageType.Contact => "👤",
            MessageType.Poll => "📊",
            _ => "📎"
        };

        preview = $"{typeIndicator} ";

        // Add message content preview (first 50 characters)
        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            var content = message.Content.Length > 50 
                ? message.Content.Substring(0, 50) + "..." 
                : message.Content;
            
            // Replace newlines with spaces for single-line display
            content = content.Replace('\n', ' ').Replace('\r', ' ');
            preview += content;
        }
        else if (message.Media != null && !string.IsNullOrWhiteSpace(message.Media.FileName))
        {
            preview += message.Media.FileName;
        }
        else
        {
            preview += message.MessageType.ToString();
        }

        return preview;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("MessageDataToPreviewConverter does not support ConvertBack");
    }
}