using System.IO;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LockScreenLive.Models;
using LockScreenLive.Native;
using LockScreenLive.Views;

namespace LockScreenLive.Services;

/// <summary>
/// Shows the video overlay on every monitor; on the first user input it locks the workstation for real
/// and tears the overlay down once the lock screen has taken over.
/// </summary>
internal sealed class OverlayController : IDisposable
{
    private static readonly TimeSpan CloseDelayAfterLock = TimeSpan.FromMilliseconds(600);

    private readonly List<VideoLockWindow> _windows = new();
    private readonly InputGuard _inputGuard = new();
    private readonly DispatcherTimer _maxDurationTimer = new();
    private readonly DispatcherTimer _closeTimer = new() { Interval = CloseDelayAfterLock };
    private LibVLC? _libVlc;
    private int _cursorHides;
    private bool _locking;

    public bool IsShowing => _windows.Count > 0;

    public OverlayController()
    {
        _inputGuard.InputDetected += LockNow;
        _maxDurationTimer.Tick += (_, _) =>
        {
            Log.Info("Max overlay duration reached");
            LockNow();
        };
        _closeTimer.Tick += (_, _) => Close();
    }

    public bool Show(AppSettings settings)
    {
        if (IsShowing)
            return true;
        if (!File.Exists(settings.VideoPath))
        {
            Log.Error($"Video not found: '{settings.VideoPath}'");
            return false;
        }

        _libVlc ??= CreateLibVlc();

        var screens = System.Windows.Forms.Screen.AllScreens;
        foreach (var screen in screens)
        {
            if (!settings.AllMonitors && !screen.Primary)
                continue;
            var window = new VideoLockWindow(_libVlc, screen.Bounds, settings, withAudio: screen.Primary && !settings.Muted);
            _windows.Add(window);
            window.Show();
        }
        _windows.FirstOrDefault(w => w.IsPrimaryAudio)?.Activate();

        while (NativeMethods.ShowCursor(false) >= 0)
            _cursorHides++;
        _cursorHides++;

        _locking = false;
        _inputGuard.Start();
        _maxDurationTimer.Interval = TimeSpan.FromMinutes(settings.MaxOverlayMinutes);
        _maxDurationTimer.Start();
        Log.Info($"Overlay shown on {_windows.Count} screen(s)");
        return true;
    }

    private void LockNow()
    {
        if (!IsShowing || _locking)
            return;
        _locking = true;
        _maxDurationTimer.Stop();

        if (NativeMethods.LockWorkStation())
        {
            // Keep the video up until the lock screen has painted over it, so the desktop never flashes.
            _closeTimer.Start();
        }
        else
        {
            Log.Error($"LockWorkStation failed (error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()})");
            Close();
        }
    }

    /// <summary>Called when the session actually locks, or from the close timer.</summary>
    public void Close()
    {
        _closeTimer.Stop();
        _maxDurationTimer.Stop();
        _inputGuard.Stop();

        foreach (var window in _windows)
            window.Close();
        _windows.Clear();

        for (; _cursorHides > 0; _cursorHides--)
            NativeMethods.ShowCursor(true);
        _locking = false;
    }

    private static LibVLC CreateLibVlc()
    {
        Core.Initialize();
        return new LibVLC("--no-video-title-show", "--no-osd", "--quiet");
    }

    public void Dispose()
    {
        Close();
        _inputGuard.Dispose();
        _libVlc?.Dispose();
    }
}
