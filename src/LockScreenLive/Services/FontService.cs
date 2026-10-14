using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using LockScreenLive.Models;

namespace LockScreenLive.Services;

/// <param name="Key">What's saved in <see cref="AppSettings.WidgetFont"/>.</param>
/// <param name="Family">Null for "style default", so the item shows in the regular UI font.</param>
public sealed record FontChoice(string Key, string Display, FontFamily? Family);

/// <summary>
/// Widget fonts. A saved font key is one of:
/// "" (the widget's default), "bundled:&lt;family&gt;", "path:&lt;font file&gt;#&lt;family&gt;", or a system font family name.
/// </summary>
internal static class FontService
{
    private const string BundledPrefix = "bundled:";
    private const string PathPrefix = "path:";
    private static readonly Uri PackBase = new("pack://application:,,,/");
    private static readonly string[] BundledFamilies = ["Michroma"];
    private static readonly string[] FontExtensions = [".ttf", ".otf", ".ttc"];

    public static FontFamily WindowsDefault { get; } = new("Segoe UI Variable Display, Segoe UI");

    private static string ImportedFontsDir
    {
        get
        {
            var dir = Path.Combine(AppPaths.DataDir, "fonts");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    // Fonts installed "for this user only" live here, and WPF's system font list doesn't always include them.
    private static string UserFontsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Fonts");

    public static FontFamily Resolve(string key, WidgetStyle style)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return style == WidgetStyle.DaySpaced ? Bundled("Michroma") : WindowsDefault;
            if (key.StartsWith(BundledPrefix, StringComparison.Ordinal))
                return Bundled(key[BundledPrefix.Length..]);
            if (key.StartsWith(PathPrefix, StringComparison.Ordinal))
            {
                var value = key[PathPrefix.Length..];
                int hash = value.LastIndexOf('#');
                if (hash > 0 && File.Exists(value[..hash]))
                    return FromFile(value[..hash], value[(hash + 1)..]);
                return WindowsDefault;
            }
            return new FontFamily(key);
        }
        catch (Exception ex)
        {
            Log.Error($"Couldn't load font '{key}'", ex);
            return WindowsDefault;
        }
    }

    public static ObservableCollection<FontChoice> GetChoices()
    {
        var choices = new ObservableCollection<FontChoice> { new("", "Style default", null) };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in BundledFamilies)
        {
            choices.Add(new($"{BundledPrefix}{name}", $"{name} (bundled)", Bundled(name)));
            seen.Add(name);
        }

        foreach (var dir in new[] { ImportedFontsDir, UserFontsDir })
            foreach (var choice in FromFolder(dir))
                if (seen.Add(choice.Display))
                    choices.Add(choice);

        foreach (var family in Fonts.SystemFontFamilies.OrderBy(f => f.Source, StringComparer.OrdinalIgnoreCase))
            if (seen.Add(family.Source))
                choices.Add(new(family.Source, family.Source, family));

        return choices;
    }

    /// <summary>Copies a font file into the app's data folder so it keeps working if the original moves.</summary>
    /// <returns>The first family in the file, or null if it contains no usable font.</returns>
    public static FontChoice? Import(string sourceFile)
    {
        var fileName = string.Concat(Path.GetFileName(sourceFile).Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
        var target = Path.Combine(ImportedFontsDir, fileName);
        File.Copy(sourceFile, target, overwrite: true);
        return ChoicesFromFile(target).FirstOrDefault();
    }

    private static FontFamily Bundled(string name) => new(PackBase, $"./Fonts/#{name}");

    private static FontFamily FromFile(string file, string family) =>
        new(new Uri(Path.GetDirectoryName(file)! + Path.DirectorySeparatorChar), $"./{Path.GetFileName(file)}#{family}");

    private static IEnumerable<FontChoice> FromFolder(string dir)
    {
        if (!Directory.Exists(dir))
            return [];
        return Directory.EnumerateFiles(dir)
            .Where(f => FontExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .SelectMany(ChoicesFromFile);
    }

    private static IEnumerable<FontChoice> ChoicesFromFile(string file)
    {
        ICollection<FontFamily> families;
        try
        {
            families = Fonts.GetFontFamilies(file);
        }
        catch (Exception ex)
        {
            Log.Error($"Couldn't read font file '{file}'", ex);
            return [];
        }
        return families.Select(f =>
        {
            var name = f.Source[(f.Source.LastIndexOf('#') + 1)..];
            return new FontChoice($"{PathPrefix}{file}#{name}", name, FromFile(file, name));
        });
    }
}
