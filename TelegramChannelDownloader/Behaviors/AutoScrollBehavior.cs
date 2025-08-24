using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace TelegramChannelDownloader.Behaviors;

/// <summary>
/// Attached behavior that automatically scrolls a ScrollViewer to the bottom when items are added
/// </summary>
public static class AutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(AutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollProperty, value);
    }

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            if ((bool)e.NewValue)
            {
                // Find the ItemsControl within the ScrollViewer
                var itemsControl = FindItemsControl(scrollViewer);
                if (itemsControl?.ItemsSource is INotifyCollectionChanged collection)
                {
                    collection.CollectionChanged += (sender, args) =>
                    {
                        if (args.Action == NotifyCollectionChangedAction.Add)
                        {
                            scrollViewer.ScrollToBottom();
                        }
                    };
                }
            }
        }
    }

    private static ItemsControl? FindItemsControl(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is ItemsControl itemsControl)
            {
                return itemsControl;
            }
            var result = FindItemsControl(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}