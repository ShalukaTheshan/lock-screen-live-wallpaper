using System.Windows.Threading;

namespace LockScreenLive.Services;

/// <summary>Raises <see cref="IdleReached"/> once per idle period when the configured threshold passes.</summary>
internal sealed class IdleMonitor : IDisposable
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Func<int> _getIdleMinutes;
    private readonly Func<bool> _canTrigger;
    private bool _firedThisPeriod;

    public event Action? IdleReached;

    public IdleMonitor(Func<int> getIdleMinutes, Func<bool> canTrigger)
    {
        _getIdleMinutes = getIdleMinutes;
        _canTrigger = canTrigger;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        int minutes = _getIdleMinutes();
        if (minutes <= 0)
            return;

        var idle = SystemState.GetIdleTime();
        if (idle < TimeSpan.FromMinutes(minutes))
        {
            _firedThisPeriod = false;
            return;
        }

        // Keep checking until conditions allow it (e.g. a full-screen game closes while still idle).
        if (!_firedThisPeriod && _canTrigger())
        {
            _firedThisPeriod = true;
            IdleReached?.Invoke();
        }
    }

    public void Dispose() => _timer.Stop();
}
