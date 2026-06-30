using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LunaChat.Views;

/// <summary>true → accent green, false → error red (kiro status dot).</summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public static readonly BoolToStatusColorConverter Instance = new();

    private static readonly Color Ready = Color.Parse("#19C37D");
    private static readonly Color NotReady = Color.Parse("#F2555A");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Ready : NotReady;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → green brush, false → red brush (SVG status fill).</summary>
public class BoolToStatusBrushConverter : IValueConverter
{
    public static readonly BoolToStatusBrushConverter Instance = new();

    private static readonly IBrush Ready = new SolidColorBrush(Color.Parse("#19C37D"));
    private static readonly IBrush NotReady = new SolidColorBrush(Color.Parse("#F2555A"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Ready : NotReady;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → expanded sidebar width, false → 0 (collapsed).</summary>
public class BoolToSidebarWidthConverter : IValueConverter
{
    public static readonly BoolToSidebarWidthConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new GridLength(264) : new GridLength(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → right inset so conversation clears the floating card, false → none.</summary>
public class BoolToConversationInsetConverter : IValueConverter
{
    public static readonly BoolToConversationInsetConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new Thickness(0, 4, 284, 8) : new Thickness(0, 4, 0, 8);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → "Stop", false → "Send".</summary>
public class RunningToSendLabelConverter : IValueConverter
{
    public static readonly RunningToSendLabelConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Stop" : "Send";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true (user) → accent green, false (kiro) → secondary gray.</summary>
public class RoleColorConverter : IValueConverter
{
    public static readonly RoleColorConverter Instance = new();

    private static readonly IBrush User = new SolidColorBrush(Color.Parse("#00D5A0"));
    private static readonly IBrush Kiro = new SolidColorBrush(Color.Parse("#888888"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? User : Kiro;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → danger red brush, false → accent brush (Send/Stop button bg).</summary>
public class RunningToButtonBrushConverter : IValueConverter
{
    public static readonly RunningToButtonBrushConverter Instance = new();

    private static readonly IBrush Stop = new SolidColorBrush(Color.Parse("#FF6B6B"));
    private static readonly IBrush Send = new SolidColorBrush(Color.Parse("#00D5A0"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Stop : Send;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
