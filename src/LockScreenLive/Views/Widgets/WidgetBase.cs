using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace LockScreenLive.Views.Widgets;

/// <summary>Shared clock tick, colours and fade-in for widgets. Widgets are laid out for a 1080px-tall screen.</summary>
public class WidgetBase : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    protected WidgetOptions? Options { get; private set; }

    /// <summary>Fade in when shown (on the lock overlay, not in the settings previews).</summary>
    public bool AnimateIn { get; set; }

    public WidgetBase()
    {
        IsHitTestVisible = false;
        _timer.Tick += (_, _) => UpdateTime(DateTime.Now);
        Loaded += OnLoaded;
        Unloaded += (_, _) => _timer.Stop();
    }

    public void Apply(WidgetOptions options)
    {
        Options = options;
        Foreground = new SolidColorBrush(options.Color);
        // Dark text reads better with a light glow than a dark shadow.
        bool darkText = options.Color.R + options.Color.G + options.Color.B < 3 * 128;
        Effect = new DropShadowEffect
        {
            BlurRadius = 18,
            ShadowDepth = 0,
            Color = darkText ? Colors.White : Colors.Black,
            Opacity = darkText ? 0.35 : 0.45,
        };
        OnOptionsChanged(options);
        UpdateTime(DateTime.Now);
    }

    protected virtual void OnOptionsChanged(WidgetOptions options)
    {
    }

    protected virtual void UpdateTime(DateTime now)
    {
    }

    protected string FormatTime(DateTime now)
    {
        if (Options?.Use24Hour == true)
            return now.ToString("HH:mm", CultureInfo.CurrentCulture);
        // Like Windows, show the short time without an AM/PM designator.
        var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Replace("tt", "").Trim();
        return now.ToString(pattern, CultureInfo.CurrentCulture);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTime(DateTime.Now);
        _timer.Start();
        if (AnimateIn)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
        }
    }
}
