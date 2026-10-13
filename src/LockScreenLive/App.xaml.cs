using System.IO;
using System.Windows;
using LockScreenLive.Models;
using LockScreenLive.Services;
using LockScreenLive.Views;
using Microsoft.Win32;

namespace LockScreenLive;

public partial class App : Application
{
    private const string MutexName = @"Local\LockScreenLive.SingleInstance";
    private const string ShowSettingsEventName = @"Local\LockScreenLive.ShowSettings";

    private Mutex? _mutex;
    private EventWaitHandle? _showSettingsEvent;
    private RegisteredWaitHandle? _showSettingsWait;
    private OverlayController? _overlay;
    private IdleMonitor? _idleMonitor;
    private HotkeyService? _hotkey;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;
    private bool _sessionLocked;
    private bool _idlePaused;

    internal AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            // Already running: ask that instance to show its settings window.
            if (EventWaitHandle.TryOpenExisting(ShowSettingsEventName, out var existing))
                existing.Set();
            Shutdown();
            return;
        }
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(_showSettingsEvent,
            (_, _) => Dispatcher.BeginInvoke(OpenSettings), null, Timeout.Infinite, executeOnlyOnce: false);

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error("Unhandled exception", args.Exception);
            _overlay?.Close();
            args.Handled = true;
        };

        Settings = SettingsService.Load();

        _overlay = new OverlayController();
        _hotkey = new HotkeyService();
        _hotkey.Pressed += () => ShowOverlay(Settings);
        _idleMonitor = new IdleMonitor(() => Settings.IdleMinutes, CanAutoTrigger);
        _idleMonitor.IdleReached += () => ShowOverlay(Settings);

        _tray = new TrayIcon();
        _tray.ShowNowClicked += () => ShowOverlay(Settings);
        _tray.SettingsClicked += OpenSettings;
        _tray.PauseToggled += paused => _idlePaused = paused;
        _tray.ExitClicked += Shutdown;

        SystemEvents.SessionSwitch += OnSessionSwitch;

        RegisterHotkey();
        Log.Info("Started");

        bool background = e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        if (!background || !File.Exists(Settings.VideoPath))
            OpenSettings();
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    _sessionLocked = true;
                    _overlay?.Close();
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _sessionLocked = false;
                    break;
            }
        });
    }

    private bool CanAutoTrigger()
    {
        if (_idlePaused || _sessionLocked || _overlay is null || _overlay.IsShowing || _settingsWindow is { IsActive: true })
            return false;
        if (!File.Exists(Settings.VideoPath))
            return false;
        if (Settings.SkipOnBattery && SystemState.IsOnBattery())
            return false;
        if (Settings.SkipWhenFullscreen && SystemState.IsFullscreenAppActive())
            return false;
        if (Settings.SkipWhenDisplayRequired && SystemState.IsDisplayRequired())
            return false;
        return true;
    }

    internal void ShowOverlay(AppSettings settings)
    {
        if (_overlay is null || _sessionLocked || _overlay.IsShowing)
            return;
        if (!_overlay.Show(settings))
            _tray?.Notify("Lock Screen Live", "Couldn't play the video. Choose a valid video file in Settings.", isError: true);
    }

    internal void SuspendHotkey(bool suspended)
    {
        if (_hotkey is not null)
            _hotkey.Suspended = suspended;
    }

    internal async void ApplySettings(AppSettings settings)
    {
        settings.Normalize();
        Settings = settings;
        RegisterHotkey();
        StartupService.Apply(settings.StartWithWindows);
        SettingsService.Save(settings);

        if (!settings.SyncLockImage || !File.Exists(settings.VideoPath))
            return;
        var key = LockImageService.KeyFor(settings.VideoPath, settings.LockImageFrameSeconds);
        if (key == settings.AppliedLockImageKey)
            return;
        try
        {
            await LockImageService.ApplyAsync(settings.VideoPath, settings.LockImageFrameSeconds);
            settings.AppliedLockImageKey = key;
            SettingsService.Save(settings);
            _tray?.Notify("Lock Screen Live", "Lock screen image updated to match your video.");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to set lock screen image", ex);
            var result = MessageBox.Show(
                "Windows didn't allow the app to change the lock screen image.\n\n" +
                "You can set it yourself: open Lock screen settings, choose \"Picture\", and browse to:\n" +
                $"{AppPaths.DataDir}\n\nOpen Lock screen settings now?",
                "Lock Screen Live", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
                OpenUri("ms-settings:lockscreen");
        }
    }

    private void RegisterHotkey()
    {
        if (_hotkey is null || string.IsNullOrWhiteSpace(Settings.Hotkey))
            return;
        if (!_hotkey.Register(Settings.Hotkey))
            _tray?.Notify("Lock Screen Live", $"The hotkey {Settings.Hotkey} is unavailable. Choose another in Settings.", isError: true);
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(Settings);
        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            SuspendHotkey(false);
        };
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    internal static void OpenUri(string uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open {uri}", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _showSettingsWait?.Unregister(null);
        _showSettingsEvent?.Dispose();
        _idleMonitor?.Dispose();
        _hotkey?.Dispose();
        _overlay?.Dispose();
        _tray?.Dispose();
        _mutex?.Dispose();
        Log.Info("Exited");
        base.OnExit(e);
    }
}
