using System.Windows;
using System.Windows.Interop;
using LibVLCSharp.Shared;
using LockScreenLive.Models;
using LockScreenLive.Native;
using LockScreenLive.Services;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace LockScreenLive.Views;

public partial class VideoLockWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly System.Drawing.Rectangle _bounds;
    private readonly AppSettings _settings;
    private MediaPlayer? _player;
    private Media? _media;

    public bool IsPrimaryAudio { get; }

    internal VideoLockWindow(LibVLC libVlc, System.Drawing.Rectangle bounds, AppSettings settings, bool withAudio)
    {
        InitializeComponent();
        _libVlc = libVlc;
        _bounds = bounds;
        _settings = settings;
        IsPrimaryAudio = withAudio;

        // Rough placement in DIPs; SourceInitialized snaps to exact physical pixels.
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        Clock.Visibility = settings.ShowClock ? Visibility.Visible : Visibility.Collapsed;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, _bounds.Left, _bounds.Top, _bounds.Width, _bounds.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _player = new MediaPlayer(_libVlc) { EnableHardwareDecoding = true, Mute = !IsPrimaryAudio };
            if (_settings.FillScreen)
                _player.CropGeometry = $"{_bounds.Width}:{_bounds.Height}";
            VideoView.MediaPlayer = _player;

            _media = new Media(_libVlc, _settings.VideoPath, FromType.FromPath);
            _media.AddOption(":input-repeat=65535");
            if (!IsPrimaryAudio)
                _media.AddOption(":no-audio");
            _player.Play(_media);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to start video playback", ex);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        VideoView.MediaPlayer = null;
        var player = _player;
        var media = _media;
        _player = null;
        _media = null;
        // Stopping libVLC can block for a moment; keep it off the UI thread.
        Task.Run(() =>
        {
            player?.Stop();
            player?.Dispose();
            media?.Dispose();
        });
        VideoView.Dispose();
    }
}
