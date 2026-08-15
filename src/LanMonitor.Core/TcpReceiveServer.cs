using System.Net;
using System.Net.Sockets;

namespace LanMonitor.Core;

public sealed class TcpReceiveServer : IDisposable
{
    private readonly object _gate = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private TcpClient? _currentTcp;
    private int _accepting;

    public int Port { get; set; } = 19730;
    public int BoundPort { get; private set; }
    public string AllowList { get; set; } = string.Empty;

    public string? CurrentClient { get; private set; }

    public event Action<string>? Log;
    public event Action<string?>? ClientChanged;
    public event Action<byte[]>? FrameReceived;

    public bool IsListening => _listener is not null;

    public void Start()
    {
        lock (_gate)
        {
            if (_listener is not null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            BoundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = AcceptLoopAsync(_cts.Token);
        }

        RaiseLog($"已开始监听端口 {Port}");
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        TcpListener? listener;
        TcpClient? tcp;
        lock (_gate)
        {
            cts = _cts;
            listener = _listener;
            _cts = null;
            _listener = null;
            CurrentClient = null;
            tcp = _currentTcp;
            _currentTcp = null;
        }

        try { tcp?.Close(); } catch { /* ignore */ }
        try { cts?.Cancel(); } catch { /* ignore */ }
        try { listener?.Stop(); } catch { /* ignore */ }
        cts?.Dispose();
        ClientChanged?.Invoke(null);
        RaiseLog("已停止监听");
    }

    public void DisconnectCurrent()
    {
        TcpClient? tcp;
        lock (_gate)
        {
            tcp = _currentTcp;
            _currentTcp = null;
            CurrentClient = null;
        }

        try { tcp?.Close(); } catch { /* ignore */ }
        ClientChanged?.Invoke(null);
        RaiseLog("已断开当前客户");
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpListener? listener;
            lock (_gate) { listener = _listener; }
            if (listener is null)
            {
                break;
            }

            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                RaiseLog("接受连接失败: " + ex.Message);
                continue;
            }

            _ = HandleClientAsync(client, token);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "";
            if (!IpFilter.IsAllowed(endpoint, AllowList))
            {
                RaiseLog("拒绝未授权 " + endpoint);
                return;
            }

            if (Interlocked.CompareExchange(ref _accepting, 1, 0) != 0)
            {
                RaiseLog("已有客户在线，拒绝 " + endpoint);
                return;
            }

            try
            {
                lock (_gate) { _currentTcp = client; }
                CurrentClient = endpoint;
                ClientChanged?.Invoke(endpoint);
                RaiseLog("用户【" + endpoint + "】已上线");

                var assembler = new FrameAssembler();
                var buffer = new byte[64 * 1024];
                var stream = client.GetStream();
                while (!token.IsCancellationRequested && client.Connected)
                {
                    int read;
                    try
                    {
                        read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        break;
                    }

                    if (read <= 0)
                    {
                        break;
                    }

                    var frames = assembler.Push(buffer.AsSpan(0, read));
                    foreach (var frame in frames)
                    {
                        FrameReceived?.Invoke(frame);
                    }
                }
            }
            finally
            {
                RaiseLog("用户【" + endpoint + "】连接已断开");
                lock (_gate)
                {
                    if (ReferenceEquals(_currentTcp, client))
                    {
                        _currentTcp = null;
                    }
                }

                CurrentClient = null;
                ClientChanged?.Invoke(null);
                Interlocked.Exchange(ref _accepting, 0);
            }
        }
    }

    private void RaiseLog(string message) => Log?.Invoke(message);

    public void Dispose() => Stop();
}
