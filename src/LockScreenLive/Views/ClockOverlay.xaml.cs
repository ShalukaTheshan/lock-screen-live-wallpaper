using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace LockScreenLive.Views;

/// <summary>Time and date styled after the Windows 11 lock screen.</summary>
public partial class ClockOverlay : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly string _timeFormat;

    public ClockOverlay()
    {
        InitializeComponent();
        // Windows shows the short time without an AM/PM designator.
        _timeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Replace("tt", "").Trim();
        _timer.Tick += (_, _) => UpdateText();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _timer.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateText();
        _timer.Start();
        ClockPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        });
    }

    private void UpdateText()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString(_timeFormat, CultureInfo.CurrentCulture);
        DateText.Text = now.ToString("dddd, MMMM d", CultureInfo.CurrentCulture);
    }
}
