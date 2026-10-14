using System.Globalization;

namespace LockScreenLive.Views.Widgets;

/// <summary>Time and date styled after the Windows 11 lock screen.</summary>
public partial class Windows11ClockWidget : WidgetBase
{
    public Windows11ClockWidget()
    {
        InitializeComponent();
    }

    protected override void OnOptionsChanged(WidgetOptions options)
    {
        TimeText.FontFamily = options.Font;
        DateText.FontFamily = options.Font;
    }

    protected override void UpdateTime(DateTime now)
    {
        TimeText.Text = FormatTime(now);
        DateText.Text = now.ToString("dddd, MMMM d", CultureInfo.CurrentCulture);
    }
}
