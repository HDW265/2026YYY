using System.Net.Sockets;
using LanMonitor.Core;

namespace LanMonitor.Sender;

internal enum SenderState
{
    Idle,
    Connecting,
    Connected,
    Reconnecting,
    StoppedAtLimit
}

internal sealed class StreamSession : IDisposable
{
    private readonly object _gate = new();
    private readonly ReconnectGate _reconnect = new() { Unlimited = true };
    private CancellationTokenSource? _cts;
    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private Task? _loopTask;
    private volatile bool _streaming = true;
    private volatile int _fps = 5;
    private volatile int _quality = 60;
    private volatile int _maxEdge = 1280;
    private int _userStop;
    private int _failStreak;
    private long _lastCaptureFailLogTick;

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 19730;
    public bool AutoReconnect { get; set; } = true;

    /// <summary>基础重连间隔；持续模式下会随失败次数略微退避，上限 30s。</summary>
    public int ReconnectDelayMs { get; set; } = 3000;

    public bool ContinuousReconnect
    {
        get => _reconnect.Unlimited;
        set => _reconnect.Unlimited = value;
    }

    public int MaxReconnectAttempts
    {
        get => _reconnect.MaxAttempts;
        set => _reconnect.MaxAttempts = Math.Clamp(value, 0, 100);
    }

    public SenderState State { get; private set; } = SenderState.Idle;
    public int ReconnectAttemptsUsed => _reconnect.AttemptsUsed;
    public int ReconnectAttemptsRemaining =>
        ContinuousReconnect ? _reconnect.AttemptsUsed : _reconnect.AttemptsRemaining;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public int Fps
    {
        get => _fps;
        set => _fps = Math.Clamp(value, 1, 30);
    }

    public int Quality
    {
        get => _quality;
        set => _quality = Math.Clamp(value, 1, 100);
    }

    public int MaxEdge
    {
        get => _maxEdge;
        set => _maxEdge = Math.Clamp(value, 0, 7680);
    }

    public bool Streaming
    {
        get => _streaming;
        set => _streaming = value;
    }

    public event Action<string>? Log;
    public event Action<SenderState>? StateChanged;
    public event Action<int, double>? FrameSent;

    public void ApplySettings(SenderSettings settings)
    {
        Host = settings.Host;
        Port = settings.Port;
        Fps = settings.Fps;
        Quality = settings.Quality;
        MaxEdge = settings.MaxEdge;
        AutoReconnect = settings.AutoReconnect;
        ContinuousReconnect = settings.ContinuousReconnect;
        MaxReconnectAttempts = settings.MaxReconnectAttempts;
        Streaming = settings.Streaming;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_cts is not null)
            {
                return;
            }

            _userStop = 0;
            _failStreak = 0;
            _reconnect.Reset();
            _cts = new CancellationTokenSource();
            var cts = _cts;
            _loopTask = Task.Run(() => RunAsync(cts));
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        TcpClient? tcp;
        lock (_gate)
        {
            Interlocked.Exchange(ref _userStop, 1);
            cts = _cts;
            _cts = null;
            tcp = _tcp;
            _tcp = null;
            _stream = null;
        }

        try { tcp?.Close(); } catch { /* ignore */ }
        try { cts?.Cancel(); } catch { /* ignore */ }
        try { cts?.Dispose(); } catch { /* ignore */ }
        SetState(SenderState.Idle);
        RaiseLog("已断开。");
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationTokenSource ownedCts)
    {
        var token = ownedCts.Token;
        var first = true;
        try
        {
            while (!token.IsCancellationRequested && Volatile.Read(ref _userStop) == 0)
            {
                if (!first)
                {
                    if (!AutoReconnect || !_reconnect.TryBeginAttempt())
                    {
                        SetState(SenderState.StoppedAtLimit);
                        RaiseLog(AutoReconnect
                            ? $"已达重连上限 {_reconnect.MaxAttempts}，停止。"
                            : "自动重连已关，停止。");
                        break;
                    }

                    var delay = NextDelayMs();
                    SetState(SenderState.Reconnecting);
                    RaiseLog(ContinuousReconnect
                        ? $"持续重连 第 {_reconnect.AttemptsUsed} 次，{delay}ms 后重试…"
                        : $"重连 {_reconnect.AttemptsUsed}/{_reconnect.MaxAttempts}，{delay}ms 后重试…");
                    try
                    {
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                first = false;
                SetState(SenderState.Connecting);
                RaiseLog($"连接 {Host}:{Port} …");

                TcpClient? tcp = null;
                try
                {
                    tcp = new TcpClient();
                    using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    connectCts.CancelAfter(TimeSpan.FromSeconds(8));
                    await tcp.ConnectAsync(Host, Port, connectCts.Token).ConfigureAwait(false);
                    tcp.NoDelay = true;

                    lock (_gate)
                    {
                        if (token.IsCancellationRequested || Volatile.Read(ref _userStop) != 0)
                        {
                            tcp.Close();
                            return;
                        }

                        _tcp = tcp;
                        _stream = tcp.GetStream();
                        tcp = null;
                    }

                    _failStreak = 0;
                    _reconnect.Reset();
                    SetState(SenderState.Connected);
                    RaiseLog("已连接，开始推流。");
                    await CaptureLoopAsync(token).ConfigureAwait(false);
                    _failStreak++;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _failStreak++;
                    RaiseLog("连接/发送失败: " + ex.Message);
                }
                finally
                {
                    CloseSocket();
                    tcp?.Dispose();
                }
            }
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_cts, ownedCts))
                {
                    _cts = null;
                    _loopTask = null;
                }
            }

            if (Volatile.Read(ref _userStop) != 0)
            {
                SetState(SenderState.Idle);
            }
            else if (State != SenderState.StoppedAtLimit)
            {
                SetState(SenderState.Idle);
            }
        }
    }

    private int NextDelayMs()
    {
        var baseMs = Math.Max(500, ReconnectDelayMs);
        if (!ContinuousReconnect)
        {
            return baseMs;
        }

        // 3s → 6s → 12s → 24s → 封顶 30s
        var shift = Math.Clamp(_failStreak, 0, 3);
        var delay = baseMs * (1 << shift);
        return Math.Min(30_000, delay);
    }

    private async Task CaptureLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && Volatile.Read(ref _userStop) == 0)
        {
            var delayMs = Math.Max(33, 1000 / Math.Max(1, Fps));
            if (!Streaming)
            {
                await Task.Delay(delayMs, token).ConfigureAwait(false);
                continue;
            }

            byte[] jpeg;
            try
            {
                jpeg = ScreenCapture.CapturePrimaryJpeg(Quality, MaxEdge);
            }
            catch (Exception ex)
            {
                // 避免锁屏等场景刷屏
                if (Environment.TickCount64 - _lastCaptureFailLogTick > 5000)
                {
                    _lastCaptureFailLogTick = Environment.TickCount64;
                    RaiseLog("截屏失败: " + ex.Message);
                }

                await Task.Delay(delayMs, token).ConfigureAwait(false);
                continue;
            }

            NetworkStream? stream;
            lock (_gate)
            {
                stream = _stream;
            }

            if (stream is null)
            {
                return;
            }

            try
            {
                var started = Environment.TickCount64;
                await FramePacket.WriteAsync(stream, jpeg, token).ConfigureAwait(false);
                var elapsed = Math.Max(1, Environment.TickCount64 - started);
                FrameSent?.Invoke(jpeg.Length, 1000.0 / delayMs);
                var sleep = delayMs - (int)elapsed;
                if (sleep > 0)
                {
                    await Task.Delay(sleep, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                RaiseLog("发送中断: " + ex.Message);
                return;
            }
        }
    }

    private void CloseSocket()
    {
        TcpClient? tcp;
        lock (_gate)
        {
            tcp = _tcp;
            _tcp = null;
            _stream = null;
        }

        try { tcp?.Close(); } catch { /* ignore */ }
    }

    private void SetState(SenderState state)
    {
        State = state;
        try { StateChanged?.Invoke(state); } catch { /* ignore */ }
    }

    private void RaiseLog(string message)
    {
        try { Log?.Invoke(message); } catch { /* ignore */ }
    }
}
