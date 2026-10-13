using System.Windows.Input;
using System.Windows.Interop;
using LockScreenLive.Native;

namespace LockScreenLive.Services;

/// <summary>A single global hotkey registered on a hidden message-only window.</summary>
internal sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x4C53;
    private readonly HwndSource _source;
    private bool _registered;

    public event Action? Pressed;

    /// <summary>While true, presses are ignored (used while the settings window captures a new hotkey).</summary>
    public bool Suspended { get; set; }

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("LockScreenLiveHotkey")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
            Width = 0,
            Height = 0,
            WindowStyle = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    /// <returns>False if the text can't be parsed or another app already owns the combination.</returns>
    public bool Register(string hotkey)
    {
        Unregister();
        if (!TryParse(hotkey, out uint modifiers, out uint vk))
            return false;
        _registered = NativeMethods.RegisterHotKey(_source.Handle, HotkeyId, modifiers | NativeMethods.MOD_NOREPEAT, vk);
        if (!_registered)
            Log.Error($"RegisterHotKey failed for '{hotkey}' (error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()})");
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
            _registered = false;
        }
    }

    public static bool TryParse(string text, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts[..^1])
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= NativeMethods.MOD_CONTROL; break;
                case "alt": modifiers |= NativeMethods.MOD_ALT; break;
                case "shift": modifiers |= NativeMethods.MOD_SHIFT; break;
                case "win" or "windows": modifiers |= NativeMethods.MOD_WIN; break;
                default: return false;
            }
        }

        if (modifiers == 0 || !Enum.TryParse(parts[^1], ignoreCase: true, out Key key) || key == Key.None)
            return false;
        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return vk != 0;
    }

    public static string Format(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            if (!Suspended)
                Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
