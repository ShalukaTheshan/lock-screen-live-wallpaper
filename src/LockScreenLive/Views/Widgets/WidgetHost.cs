using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LockScreenLive.Models;

namespace LockScreenLive.Views.Widgets;

/// <summary>
/// Places a widget on a screen-shaped area. Sizes and margins scale with the area's height (designed for 1080px), so the
/// full-screen overlay and the small previews in Settings look identical.
/// </summary>
public sealed class WidgetHost : Grid
{
    private const double DesignHeight = 1080;
    private WidgetBase? _widget;
    private WidgetOptions? _options;

    public bool AnimateIn { get; set; }

    public WidgetHost()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
        SizeChanged += (_, _) => UpdateLayoutForSize();
    }

    public WidgetOptions? Options
    {
        get => _options;
        set
        {
            bool styleChanged = value?.Style != _options?.Style || _widget is null;
            _options = value;
            if (styleChanged)
                CreateWidget();
            else if (value is not null)
                _widget?.Apply(value);
            UpdateLayoutForSize();
        }
    }

    private void CreateWidget()
    {
        Children.Clear();
        _widget = _options?.Style switch
        {
            WidgetStyle.Windows11 => new Windows11ClockWidget(),
            WidgetStyle.DaySpaced => new DaySpacedWidget(),
            _ => null,
        };
        if (_widget is null)
            return;
        _widget.AnimateIn = AnimateIn;
        _widget.Apply(_options!);
        Children.Add(_widget);
    }

    private void UpdateLayoutForSize()
    {
        if (_widget is null || _options is null || ActualHeight <= 0)
            return;

        double k = ActualHeight / DesignHeight;
        _widget.LayoutTransform = new ScaleTransform(k * _options.Scale, k * _options.Scale);
        _widget.HorizontalAlignment = HorizontalAlignment.Center;
        (_widget.VerticalAlignment, _widget.Margin) = _options.Position switch
        {
            WidgetPosition.Top => (VerticalAlignment.Top, new Thickness(0, 96 * k, 0, 0)),
            WidgetPosition.Bottom => (VerticalAlignment.Bottom, new Thickness(0, 0, 0, 110 * k)),
            _ => (VerticalAlignment.Center, new Thickness(0)),
        };
    }
}
