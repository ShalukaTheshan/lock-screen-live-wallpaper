using System.Text.Json.Serialization;

namespace LockScreenLive.Models;

public sealed class AppSettings
{
    public string VideoPath { get; set; } = "";

    /// <summary>Show the live lock screen after this many idle minutes. 0 disables the idle trigger.</summary>
    public int IdleMinutes { get; set; } = 5;

    public string Hotkey { get; set; } = "Ctrl+Alt+L";

    public bool Muted { get; set; } = true;
    public bool AllMonitors { get; set; } = true;

    /// <summary>Crop the video to fill each screen instead of letterboxing.</summary>
    public bool FillScreen { get; set; } = true;

    /// <summary>Lock for real after the overlay has been up this long, so the display can sleep.</summary>
    public int MaxOverlayMinutes { get; set; } = 10;

    public WidgetStyle WidgetStyle { get; set; } = WidgetStyle.Windows11;

    /// <summary>Empty = the widget's own default font. See <see cref="Services.FontService"/> for the key format.</summary>
    public string WidgetFont { get; set; } = "";

    public double WidgetScale { get; set; } = 1.0;
    public WidgetPosition WidgetPosition { get; set; } = WidgetPosition.Default;
    public bool Use24Hour { get; set; } = false;
    public WidgetColor WidgetColor { get; set; } = WidgetColor.White;

    /// <summary>Replaced by <see cref="WidgetStyle"/>; only read to migrate older settings files.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShowClock { get; set; }

    public bool SyncLockImage { get; set; } = true;
    public double LockImageFrameSeconds { get; set; } = 0;

    public bool SkipWhenFullscreen { get; set; } = true;
    public bool SkipWhenDisplayRequired { get; set; } = true;
    public bool SkipOnBattery { get; set; } = false;

    public bool StartWithWindows { get; set; } = false;

    /// <summary>Identifies the video/frame last applied to the Windows lock screen, so we only re-apply on change.</summary>
    public string AppliedLockImageKey { get; set; } = "";

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public void Normalize()
    {
        VideoPath = VideoPath.Trim().Trim('"');
        IdleMinutes = Math.Clamp(IdleMinutes, 0, 240);
        MaxOverlayMinutes = Math.Clamp(MaxOverlayMinutes, 1, 240);
        LockImageFrameSeconds = Math.Max(0, LockImageFrameSeconds);
        WidgetScale = Math.Clamp(WidgetScale, 0.5, 2.0);
        if (ShowClock == false)
            WidgetStyle = WidgetStyle.None;
        ShowClock = null;
    }
}
