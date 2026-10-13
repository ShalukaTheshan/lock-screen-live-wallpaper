using System.Runtime.InteropServices;
using System.Windows.Threading;
using LockScreenLive.Native;

namespace LockScreenLive.Services;

/// <summary>
/// System-wide keyboard/mouse hooks active while the overlay is up. The first real input raises
/// <see cref="InputDetected"/> and key/button presses are swallowed so they never reach other apps.
/// </summary>
internal sealed class InputGuard : IDisposable
{
    private const int MoveThresholdPx = 20;
    private static readonly TimeSpan Grace = TimeSpan.FromMilliseconds(800);

    // Delegates must stay referenced for as long as the hooks are installed.
    private readonly NativeMethods.LowLevelProc _keyboardProc;
    private readonly NativeMethods.LowLevelProc _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private DateTime _armedAtUtc;
    private NativeMethods.POINT? _origin;
    private bool _raised;

    public event Action? InputDetected;

    public InputGuard()
    {
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    private bool Armed => DateTime.UtcNow >= _armedAtUtc;

    public void Start()
    {
        if (_keyboardHook != IntPtr.Zero)
            return;
        // The grace period ignores the tail of whatever started the overlay (hotkey release, tray click).
        _armedAtUtc = DateTime.UtcNow + Grace;
        _origin = null;
        _raised = false;

        var module = NativeMethods.GetModuleHandle(null);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);
        if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
            Log.Error($"SetWindowsHookEx failed (error {Marshal.GetLastWin32Error()})");
    }

    public void Stop()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            // Only key-downs are swallowed; letting key-ups through keeps modifier state from getting stuck.
            if (msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
            {
                if (Armed)
                    Raise();
                return new IntPtr(1);
            }
        }
        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == NativeMethods.WM_MOUSEMOVE)
            {
                if (Armed)
                {
                    var pt = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam).pt;
                    if (_origin is not { } origin)
                        _origin = pt;
                    else if (Math.Abs(pt.X - origin.X) > MoveThresholdPx || Math.Abs(pt.Y - origin.Y) > MoveThresholdPx)
                        Raise();
                }
            }
            else if (msg is NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_RBUTTONDOWN or NativeMethods.WM_MBUTTONDOWN
                     or NativeMethods.WM_XBUTTONDOWN or NativeMethods.WM_MOUSEWHEEL or NativeMethods.WM_MOUSEHWHEEL)
            {
                if (Armed)
                    Raise();
                return new IntPtr(1);
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void Raise()
    {
        if (_raised)
            return;
        _raised = true;
        // Hook callbacks must return quickly; do the real work after.
        Dispatcher.CurrentDispatcher.BeginInvoke(() => InputDetected?.Invoke());
    }

    public void Dispose() => Stop();
}
