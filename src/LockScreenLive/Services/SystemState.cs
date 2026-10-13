using LockScreenLive.Native;

namespace LockScreenLive.Services;

/// <summary>Queries used to decide whether it's a good moment to start the overlay automatically.</summary>
internal static class SystemState
{
    public static TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LASTINPUTINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>() };
        if (!NativeMethods.GetLastInputInfo(ref info))
            return TimeSpan.Zero;
        // Both values are 32-bit tick counts; unsigned subtraction handles wrap-around.
        uint elapsed = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(elapsed);
    }

    /// <summary>A full-screen game, video, or presentation is in the foreground.</summary>
    public static bool IsFullscreenAppActive()
    {
        if (NativeMethods.SHQueryUserNotificationState(out int state) != 0)
            return false;
        return state is NativeMethods.QUNS_BUSY or NativeMethods.QUNS_RUNNING_D3D_FULL_SCREEN or NativeMethods.QUNS_PRESENTATION_MODE;
    }

    /// <summary>Some app (e.g. a video player) has asked Windows to keep the display on.</summary>
    public static bool IsDisplayRequired()
    {
        uint status = NativeMethods.CallNtPowerInformation(NativeMethods.SystemExecutionState, IntPtr.Zero, 0, out uint state, sizeof(uint));
        return status == 0 && (state & NativeMethods.ES_DISPLAY_REQUIRED) != 0;
    }

    public static bool IsOnBattery() =>
        System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline;
}
