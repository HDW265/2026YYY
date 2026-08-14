namespace LanMonitor.Core;

/// <summary>
/// 保存间隔由 UI 直接改 IntervalSeconds，改完立即生效。
/// </summary>
public sealed class SaveScheduler
{
    private DateTime _lastSaveUtc = DateTime.MinValue;

    public double IntervalSeconds { get; set; } = 1.0;

    public double ClampIntervalSeconds(double seconds) =>
        Math.Clamp(seconds, 0.2, 60.0);

    public bool ShouldSave(DateTime utcNow)
    {
        var interval = TimeSpan.FromSeconds(ClampIntervalSeconds(IntervalSeconds));
        if (utcNow - _lastSaveUtc < interval)
        {
            return false;
        }

        _lastSaveUtc = utcNow;
        return true;
    }

    public void Reset() => _lastSaveUtc = DateTime.MinValue;
}
