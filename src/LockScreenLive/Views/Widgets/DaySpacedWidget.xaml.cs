using System.Globalization;

namespace LockScreenLive.Views.Widgets;

/// <summary>A wide, letter-spaced day name ("THURSDAY") with the date and time underneath.</summary>
public partial class DaySpacedWidget : WidgetBase
{
    public DaySpacedWidget()
    {
        InitializeComponent();
    }

    protected override void OnOptionsChanged(WidgetOptions options) => DayText.FontFamily = options.Font;

    protected override void UpdateTime(DateTime now)
    {
        var culture = CultureInfo.CurrentCulture;
        DayText.Text = now.ToString("dddd", culture).ToUpper(culture);
        DateText.Text = now.ToString("d MMMM, yyyy", culture).ToUpper(culture);
        TimeText.Text = FormatTime(now);
    }
}
