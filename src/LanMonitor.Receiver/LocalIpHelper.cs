using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanMonitor.Receiver;

internal static class LocalIpHelper
{
    public sealed record Result(string PrimaryDisplay, string Tooltip);

    public static Result Get()
    {
        var ips = new List<string>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var type = ni.NetworkInterfaceType;
                if (type is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    if (IPAddress.IsLoopback(ua.Address))
                    {
                        continue;
                    }

                    ips.Add(ua.Address.ToString());
                }
            }
        }
        catch
        {
            /* ignore */
        }

        ips = ips.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ips.Count == 0)
        {
            return new Result("--", "无可用 IPv4");
        }

        var preferred = ips
            .OrderBy(Score)
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var primary = preferred[0];
        var tip = preferred.Count == 1
            ? "单击复制"
            : "单击复制 · 另有 " + string.Join(" / ", preferred.Skip(1));
        return new Result(primary, tip);
    }

    private static int Score(string ip)
    {
        if (ip.StartsWith("192.168.", StringComparison.Ordinal)) return 0;
        if (ip.StartsWith("10.", StringComparison.Ordinal)) return 1;
        if (ip.StartsWith("172.", StringComparison.Ordinal))
        {
            var parts = ip.Split('.');
            if (parts.Length > 1 && int.TryParse(parts[1], out var second) && second is >= 16 and <= 31)
            {
                return 2;
            }
        }

        return 10;
    }
}
