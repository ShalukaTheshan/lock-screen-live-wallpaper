namespace LockScreenLive.Models;

public enum WidgetStyle
{
    None,
    Windows11,
    DaySpaced,
}

public enum WidgetPosition
{
    /// <summary>Whatever suits the selected widget (see <see cref="WidgetCatalog"/>).</summary>
    Default,
    Top,
    Center,
    Bottom,
}

public enum WidgetColor
{
    White,
    WarmWhite,
    Black,
}

public sealed record WidgetInfo(WidgetStyle Style, string Name, string Description, WidgetPosition DefaultPosition);

public static class WidgetCatalog
{
    public static IReadOnlyList<WidgetInfo> All { get; } =
    [
        new(WidgetStyle.None, "None", "Just the video", WidgetPosition.Center),
        new(WidgetStyle.Windows11, "Windows 11", "Large time with the date underneath, like the Windows lock screen", WidgetPosition.Top),
        new(WidgetStyle.DaySpaced, "Day Spaced", "Wide, spaced-out day name with the date and time below", WidgetPosition.Center),
    ];

    public static WidgetInfo Get(WidgetStyle style) => All.FirstOrDefault(w => w.Style == style) ?? All[0];
}
