using System.IO;

namespace LockScreenLive.Services;

internal static class AppPaths
{
    public static string DataDir { get; } = CreateDataDir();

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string LogFile => Path.Combine(DataDir, "log.txt");
    public static string PreviewImage => Path.Combine(DataDir, "preview.jpg");

    private static string CreateDataDir()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LockScreenLive");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
