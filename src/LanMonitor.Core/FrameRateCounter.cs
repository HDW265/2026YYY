namespace LanMonitor.Core;

public sealed class FrameRateCounter
{
    private readonly Queue<DateTime> _ticks = new();

    public double Fps { get; private set; }

    public void Tick()
    {
        var now = DateTime.UtcNow;
        _ticks.Enqueue(now);
        while (_ticks.Count > 0 && now - _ticks.Peek() > TimeSpan.FromSeconds(1))
        {
            _ticks.Dequeue();
        }

        Fps = _ticks.Count;
    }
}
