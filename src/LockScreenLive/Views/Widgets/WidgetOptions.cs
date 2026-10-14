using System.Windows.Media;
using LockScreenLive.Models;
using LockScreenLive.Services;

namespace LockScreenLive.Views.Widgets;

/// <summary>Everything a widget needs to draw itself, resolved from <see cref="AppSettings"/>.</summary>
public sealed record WidgetOptions(WidgetStyle Style, FontFamily Font, double Scale, WidgetPosition Position, bool Use24Hour, Color Color)
{
    /// <param name="style">Overrides the saved style (used by the picker cards to preview every widget).</param>
    public static WidgetOptions From(AppSettings settings, WidgetStyle? style = null)
    {
        var resolvedStyle = style ?? settings.WidgetStyle;
        var position = settings.WidgetPosition == WidgetPosition.Default
            ? WidgetCatalog.Get(resolvedStyle).DefaultPosition
            : settings.WidgetPosition;
        return new WidgetOptions(resolvedStyle, FontService.Resolve(settings.WidgetFont, resolvedStyle),
            settings.WidgetScale, position, settings.Use24Hour, ColorOf(settings.WidgetColor));
    }

    private static Color ColorOf(WidgetColor color) => color switch
    {
        WidgetColor.WarmWhite => Color.FromRgb(0xFF, 0xF1, 0xDC),
        WidgetColor.Black => Color.FromRgb(0x14, 0x14, 0x14),
        _ => Colors.White,
    };
}
