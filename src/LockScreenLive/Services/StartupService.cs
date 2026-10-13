using Microsoft.Win32;

namespace LockScreenLive.Services;

internal static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LockScreenLive";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled && Environment.ProcessPath is { } exe)
                key.SetValue(ValueName, $"\"{exe}\" --background");
            else if (key.GetValue(ValueName) is not null)
                key.DeleteValue(ValueName);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to update start-with-Windows entry", ex);
        }
    }
}
