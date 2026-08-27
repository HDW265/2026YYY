namespace LanMonitor.Core;

/// <summary>
/// 允许 IP 策略：全部放行，或仅白名单（逗号分隔）。
/// </summary>
public sealed class AllowIpPolicy
{
    private readonly object _gate = new();
    private readonly HashSet<string> _known = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase);
    private bool _allowAll = true;

    public bool AllowAll
    {
        get { lock (_gate) return _allowAll; }
        set { lock (_gate) _allowAll = value; }
    }

    public IReadOnlyCollection<string> KnownIps
    {
        get { lock (_gate) return _known.OrderBy(x => x).ToArray(); }
    }

    public IReadOnlyCollection<string> AllowedIps
    {
        get { lock (_gate) return _allowed.OrderBy(x => x).ToArray(); }
    }

    public void RememberEndpoint(string endpoint)
    {
        var ip = IpFilter.ExtractIp(endpoint);
        if (string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        lock (_gate)
        {
            _known.Add(ip);
        }
    }

    public void RememberIp(string ip)
    {
        ip = ip.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        lock (_gate)
        {
            _known.Add(ip);
        }
    }

    public void SetAllowed(IEnumerable<string> ips)
    {
        lock (_gate)
        {
            _allowed.Clear();
            foreach (var ip in ips)
            {
                var t = ip.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                {
                    _allowed.Add(t);
                    _known.Add(t);
                }
            }
        }
    }

    public bool IsEndpointAllowed(string endpoint)
    {
        lock (_gate)
        {
            if (_allowAll)
            {
                return true;
            }

            return IpFilter.IsAllowed(endpoint, string.Join(",", _allowed));
        }
    }

    /// <summary>供 TcpReceiveServer.AllowList 使用：空=全部。</summary>
    public string ToAllowListString()
    {
        lock (_gate)
        {
            if (_allowAll)
            {
                return string.Empty;
            }

            return string.Join(",", _allowed);
        }
    }

    public string SummaryText()
    {
        lock (_gate)
        {
            if (_allowAll)
            {
                return "全部";
            }

            return $"白名单({_allowed.Count})";
        }
    }

    public AllowIpPolicySnapshot CreateSnapshot()
    {
        lock (_gate)
        {
            return new AllowIpPolicySnapshot(
                _allowAll,
                _known.OrderBy(x => x).ToArray(),
                _allowed.OrderBy(x => x).ToArray());
        }
    }

    public void ApplySnapshot(AllowIpPolicySnapshot snapshot)
    {
        lock (_gate)
        {
            _allowAll = snapshot.AllowAll;
            _known.Clear();
            _allowed.Clear();
            foreach (var ip in snapshot.KnownIps)
            {
                var t = ip.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                {
                    _known.Add(t);
                }
            }

            foreach (var ip in snapshot.AllowedIps)
            {
                var t = ip.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                {
                    _allowed.Add(t);
                    _known.Add(t);
                }
            }
        }
    }
}

public sealed record AllowIpPolicySnapshot(
    bool AllowAll,
    IReadOnlyList<string> KnownIps,
    IReadOnlyList<string> AllowedIps);
