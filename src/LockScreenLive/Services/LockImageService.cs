using System.IO;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.System.UserProfile;

namespace LockScreenLive.Services;

/// <summary>Extracts a frame from the video and sets it as the real Windows lock screen image.</summary>
internal static class LockImageService
{
    public static string KeyFor(string videoPath, double seconds) =>
        $"{videoPath}|{seconds}|{File.GetLastWriteTimeUtc(videoPath).Ticks}";

    public static async Task<string> ExtractFrameAsync(string videoPath, double seconds, string outputPath)
    {
        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(videoPath));
        var clip = await MediaClip.CreateFromFileAsync(file);
        var props = clip.GetVideoEncodingProperties();

        var composition = new MediaComposition();
        composition.Clips.Add(clip);

        double maxSeconds = Math.Max(0, clip.OriginalDuration.TotalSeconds - 0.1);
        var time = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, maxSeconds));

        using var thumbnail = await composition.GetThumbnailAsync(
            time, (int)props.Width, (int)props.Height, VideoFramePrecision.NearestFrame);
        using var input = thumbnail.AsStreamForRead();
        using var output = File.Create(outputPath);
        await input.CopyToAsync(output);
        return outputPath;
    }

    /// <summary>Extracts the frame and applies it. Throws if Windows refuses the change.</summary>
    public static async Task ApplyAsync(string videoPath, double seconds)
    {
        // Windows caches lock screen images by file name, so write a fresh file each time and clean up old ones.
        foreach (var old in Directory.EnumerateFiles(AppPaths.DataDir, "lockscreen-*.jpg"))
        {
            try { File.Delete(old); } catch { /* still in use; try next time */ }
        }

        var imagePath = Path.Combine(AppPaths.DataDir, $"lockscreen-{DateTime.UtcNow.Ticks}.jpg");
        await ExtractFrameAsync(videoPath, seconds, imagePath);

        var image = await StorageFile.GetFileFromPathAsync(imagePath);
        await LockScreen.SetImageFileAsync(image);
        Log.Info($"Lock screen image set from '{videoPath}' at {seconds}s");
    }
}
