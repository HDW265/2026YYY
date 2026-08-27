namespace LanMonitor.Core;

/// <summary>
/// 重连闸门。Unlimited=true（默认）时一直允许重连，仅累计 AttemptsUsed；
/// Unlimited=false 时受 MaxAttempts 限制（0=不允许重连）。
/// </summary>
public sealed class ReconnectGate
{
    private int _used;

    public bool Unlimited { get; set; } = true;

    public int MaxAttempts { get; set; } = 5;

    public int AttemptsUsed => _used;

    public int AttemptsRemaining => Unlimited
        ? int.MaxValue
        : Math.Max(0, MaxAttempts - _used);

    public bool CanAttempt => Unlimited || (MaxAttempts > 0 && _used < MaxAttempts);

    public void Reset() => _used = 0;

    public bool TryBeginAttempt()
    {
        if (!CanAttempt)
        {
            return false;
        }

        _used++;
        return true;
    }
}
