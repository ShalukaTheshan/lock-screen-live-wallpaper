using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LockScreenLive.Models;
using LockScreenLive.Services;
using LockScreenLive.Views.Widgets;
using Microsoft.Win32;

namespace LockScreenLive.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _draft;
    private readonly ObservableCollection<FontChoice> _fonts;
    private readonly List<WidgetCard> _cards = new();
    private string _previewKey = "";
    private bool _initializing = true;

    private sealed record WidgetCard(WidgetStyle Style, Image Frame, WidgetHost Host);

    private static App CurrentApp => (App)Application.Current;

    internal SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        // Keep Save/Cancel on screen on small or high-DPI displays; the content scrolls instead.
        MaxHeight = SystemParameters.WorkArea.Height;
        _draft = current.Clone();
        DataContext = _draft;

        _fonts = FontService.GetChoices();
        FontBox.ItemsSource = _fonts;
        FontBox.SelectedItem = _fonts.FirstOrDefault(f => f.Key == _draft.WidgetFont) ?? _fonts[0];
        SizeSlider.Value = Math.Round(_draft.WidgetScale * 100);
        SizeLabel.Text = $"{SizeSlider.Value:0}%";
        PositionBox.SelectedIndex = (int)_draft.WidgetPosition;
        ColorBox.SelectedIndex = (int)_draft.WidgetColor;
        Use24HourBox.IsChecked = _draft.Use24Hour;
        BuildWidgetCards();

        _initializing = false;
        RefreshWidgets();
        Loaded += async (_, _) => await RefreshPreviewAsync();
    }

    // ---- Widget picker ----

    private void BuildWidgetCards()
    {
        foreach (var info in WidgetCatalog.All)
        {
            var frame = new Image { Stretch = Stretch.UniformToFill };
            var host = new WidgetHost();
            var screen = new Border
            {
                Width = 192,
                Height = 108,
                CornerRadius = new CornerRadius(4),
                ClipToBounds = true,
                Background = (Brush)FindResource("NoVideoBackground"),
                Child = new Grid { Children = { frame, host } },
            };
            var card = new StackPanel
            {
                ToolTip = info.Description,
                Children =
                {
                    screen,
                    new TextBlock { Text = info.Name, Margin = new Thickness(0, 6, 0, 2), HorizontalAlignment = HorizontalAlignment.Center },
                },
            };
            var item = new ListBoxItem { Content = card, Tag = info.Style };
            WidgetList.Items.Add(item);
            if (info.Style == _draft.WidgetStyle)
                WidgetList.SelectedItem = item;
            _cards.Add(new WidgetCard(info.Style, frame, host));
        }
    }

    private void OnWidgetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || WidgetList.SelectedItem is not ListBoxItem { Tag: WidgetStyle style })
            return;
        _draft.WidgetStyle = style;
        RefreshWidgets();
    }

    private void OnWidgetOptionChanged(object sender, RoutedEventArgs e)
    {
        SizeLabel.Text = $"{SizeSlider.Value:0}%";
        if (_initializing)
            return;
        _draft.WidgetFont = (FontBox.SelectedItem as FontChoice)?.Key ?? "";
        _draft.WidgetScale = SizeSlider.Value / 100;
        _draft.WidgetPosition = (WidgetPosition)Math.Max(0, PositionBox.SelectedIndex);
        _draft.WidgetColor = (WidgetColor)Math.Max(0, ColorBox.SelectedIndex);
        _draft.Use24Hour = Use24HourBox.IsChecked == true;
        RefreshWidgets();
    }

    private void OnLoadFont(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose a font file", Filter = "Fonts|*.ttf;*.otf;*.ttc" };
        if (dialog.ShowDialog(this) != true)
            return;

        FontChoice? choice = null;
        try
        {
            choice = FontService.Import(dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Error("Font import failed", ex);
        }
        if (choice is null)
        {
            MessageBox.Show(this, "That file doesn't contain a font Windows can use.", "Lock Screen Live",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existing = _fonts.FirstOrDefault(f => f.Key == choice.Key);
        if (existing is null)
        {
            _fonts.Insert(1, choice);
            existing = choice;
        }
        FontBox.SelectedItem = existing;
    }

    /// <summary>Redraws the big preview and every picker card with the current draft settings.</summary>
    private void RefreshWidgets()
    {
        PreviewWidget.Options = WidgetOptions.From(_draft);
        foreach (var card in _cards)
        {
            card.Frame.Source = PreviewImage.Source;
            card.Host.Options = WidgetOptions.From(_draft, card.Style);
        }
    }

    // ---- Video ----

    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a video",
            Filter = "Video files|*.mp4;*.m4v;*.mov;*.mkv;*.webm;*.wmv;*.avi|All files|*.*",
        };
        if (File.Exists(_draft.VideoPath))
            dialog.InitialDirectory = Path.GetDirectoryName(_draft.VideoPath);
        if (dialog.ShowDialog(this) != true)
            return;

        VideoPathBox.Text = dialog.FileName;
        await RefreshPreviewAsync();
    }

    private async void OnPreviewInputChanged(object sender, RoutedEventArgs e) => await RefreshPreviewAsync();

    private async Task RefreshPreviewAsync()
    {
        var path = _draft.VideoPath.Trim().Trim('"');
        if (!File.Exists(path))
        {
            _previewKey = "";
            PreviewImage.Source = null;
            PreviewStatus.Text = "No video selected";
            RefreshWidgets();
            return;
        }

        var key = LockImageService.KeyFor(path, _draft.LockImageFrameSeconds);
        if (key == _previewKey)
            return;
        _previewKey = key;
        PreviewStatus.Text = "Loading preview…";
        try
        {
            var imagePath = await LockImageService.ExtractFrameAsync(path, _draft.LockImageFrameSeconds, AppPaths.PreviewImage);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file locked
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();
            PreviewImage.Source = bitmap;
            PreviewStatus.Text = "";
        }
        catch (Exception ex)
        {
            Log.Error("Preview failed", ex);
            PreviewImage.Source = null;
            PreviewStatus.Text = "Preview unavailable. The video will still play, but Windows can't decode it for the lock screen image.";
        }
        RefreshWidgets();
    }

    // ---- Hotkey ----

    private void OnHotkeyFocus(object sender, KeyboardFocusChangedEventArgs e) => CurrentApp.SuspendHotkey(true);

    private void OnHotkeyBlur(object sender, KeyboardFocusChangedEventArgs e) => CurrentApp.SuspendHotkey(false);

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.Tab)
            return; // keep keyboard navigation working
        e.Handled = true;

        if (key is Key.Back or Key.Delete)
        {
            SetHotkey("");
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
            return;
        if (Keyboard.Modifiers == ModifierKeys.None)
            return; // a global hotkey without modifiers would hijack normal typing

        SetHotkey(HotkeyService.Format(Keyboard.Modifiers, key));
    }

    private void SetHotkey(string value)
    {
        _draft.Hotkey = value;
        // AppSettings doesn't raise change notifications, so refresh the binding by hand.
        DataContext = null;
        DataContext = _draft;
    }

    // ---- Buttons ----

    private void OnTestNow(object sender, RoutedEventArgs e)
    {
        var settings = _draft.Clone();
        settings.Normalize();
        if (!File.Exists(settings.VideoPath))
        {
            MessageBox.Show(this, "Choose a video file first.", "Lock Screen Live", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        CurrentApp.ShowOverlay(settings);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        CurrentApp.ApplySettings(_draft.Clone());
        Close();
    }
}
