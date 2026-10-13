using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LockScreenLive.Models;
using LockScreenLive.Services;
using Microsoft.Win32;

namespace LockScreenLive.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _draft;
    private string _previewKey = "";

    private static App CurrentApp => (App)Application.Current;

    internal SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        _draft = current.Clone();
        DataContext = _draft;
        Loaded += async (_, _) => await RefreshPreviewAsync();
    }

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
    }

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
