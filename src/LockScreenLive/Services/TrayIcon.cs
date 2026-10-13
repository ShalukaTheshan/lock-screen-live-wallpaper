using System.Drawing;
using System.Windows.Forms;

namespace LockScreenLive.Services;

internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _pauseItem;

    public event Action? ShowNowClicked;
    public event Action? SettingsClicked;
    public event Action<bool>? PauseToggled;
    public event Action? ExitClicked;

    public TrayIcon()
    {
        _pauseItem = new ToolStripMenuItem("Pause idle trigger") { CheckOnClick = true };
        _pauseItem.CheckedChanged += (_, _) => PauseToggled?.Invoke(_pauseItem.Checked);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show live lock screen now", null, (_, _) => ShowNowClicked?.Invoke());
        menu.Items.Add(_pauseItem);
        menu.Items.Add("Settings…", null, (_, _) => SettingsClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitClicked?.Invoke());

        _icon = new NotifyIcon
        {
            Text = "Lock Screen Live",
            Icon = LoadIcon(),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => SettingsClicked?.Invoke();
    }

    public void Notify(string title, string message, bool isError = false) =>
        _icon.ShowBalloonTip(5000, title, message, isError ? ToolTipIcon.Warning : ToolTipIcon.Info);

    private static Icon LoadIcon()
    {
        try
        {
            if (Environment.ProcessPath is { } exe && Icon.ExtractAssociatedIcon(exe) is { } icon)
                return icon;
        }
        catch
        {
            // Fall through to the stock icon.
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
