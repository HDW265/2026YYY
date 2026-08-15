namespace LanMonitor.Core;

/// <summary>
/// 自动重连次数闸门。MaxAttempts=0 表示不允许重连。
/// 用户点击「连接」时应 Reset；每次计划重连前调用 TryBeginAttempt。
/// </summary>
public sealed class ReconnectGate
{
    private int _used;

    public int MaxAttempts { get; set; } = 5;

    public int AttemptsUsed => _used;

    public int AttemptsRemaining => Math.Max(0, MaxAttempts - _used);

    public bool CanAttempt => MaxAttempts > 0 && _used < MaxAttempts;

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
