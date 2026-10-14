# Lock Screen Live

A live (MP4 video) lock screen for Windows 11.

## How it works

Windows 11's lock screen only supports still images, so this app layers the video on top of the real one:

1. When you're idle (or press the hotkey, default **Ctrl+Alt+L**), a full-screen window plays your video on every monitor, with a clock and date styled like the Windows lock screen.
2. The first key press, click, or mouse movement calls `LockWorkStation()`, and the real Windows lock screen takes over.
3. The real lock screen image is set to a frame of the same video, so the switch looks seamless.

The app lives in the system tray. Double-click the tray icon to open Settings.

## Build & run

The .NET 8 SDK is installed locally in `.dotnet\` (not on C:). `build.ps1` uses it and keeps the NuGet caches and temp files inside this folder too:

```powershell
.\build.ps1            # build
.\build.ps1 run        # build and run
.\build.ps1 publish    # self-contained app in .\publish  ->  run publish\LockScreenLive.exe
```

To reinstall the local SDK: `.\.tools\dotnet-install.ps1 -Channel 8.0 -InstallDir .\.dotnet -NoPath`
(download the script from https://dot.net/v1/dotnet-install.ps1 if `.tools\` is gone).

Note: the exe under `src\...\bin\` is framework-dependent. It only starts when `DOTNET_ROOT` points to `.dotnet\`, which `build.ps1 run` sets for you. Use the `publish\` build for normal use.

Video playback uses [LibVLC](https://www.videolan.org/vlc/libvlc.html) through LibVLCSharp. It ships with the app, so any common codec works, with hardware decoding.

## Widgets & fonts

In Settings › **Widget**, pick a widget from the cards. Each card shows that widget drawn on a frame of your video, and the big preview shows exactly what the lock screen will look like.

| Widget | Looks like |
|---|---|
| None | Just the video |
| Windows 11 | Large time with the date below, like the real lock screen |
| Day Spaced | A wide, spaced-out day name (**THURSDAY**) with the date and time below |

You can change the **font**, **size**, **position**, **color** and **24-hour time**.

- Day Spaced uses the bundled **Michroma** font by default (© The Michroma Project Authors, SIL Open Font License 1.1, see `src/LockScreenLive/Fonts/OFL.txt`).
- For the classic look, use **Anurati**. It's free for personal use, so it isn't bundled. Install it, then pick it from the Font list, or click **Load font file…** and choose the `.otf`/`.ttf`. Loaded fonts are copied to `%AppData%\LockScreenLive\fonts`.
- The widget only shows on the video. After the first input, the real Windows lock screen shows its own clock, which apps can't restyle.

## Tips

- Turn off **Windows Spotlight** (Settings › Personalization › Lock screen › choose *Picture*), or it will replace the synced image.
- Win+L still locks straight away with no video. Use the app's hotkey when you want the video.
- The app won't start the video automatically while a full-screen app is running or while an app is keeping the display on. You can change this in Settings.

## Security

While the video is showing, Windows hasn't locked yet. The app protects this window of time:

- Low-level keyboard and mouse hooks catch input system-wide and lock immediately. Key presses and clicks are swallowed, so they never reach other apps.
- The video windows are topmost on every monitor.
- After *Lock for real after N minutes* (default 10), it locks on its own.

Ctrl+Alt+Del always goes to the Windows secure screen. The video is a visual layer. The Windows lock screen is what protects your PC.

Settings and logs are stored in `%AppData%\LockScreenLive`.
