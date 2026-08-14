namespace LanMonitor.Core;

public static class IpFilter
{
    public static string ExtractIp(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return string.Empty;
        }

        var text = endpoint.Trim();
        var colon = text.LastIndexOf(':');
        if (colon > 0 && !text.StartsWith('[') && text.Count(c => c == ':') == 1)
        {
            return text[..colon];
        }

        var ipv4 = text.IndexOf(':');
        if (ipv4 > 0 && text.Take(ipv4).All(c => char.IsDigit(c) || c == '.'))
        {
            return text[..ipv4];
        }

        return text;
    }

    /// <summary>
    /// 允许列表为空则放行全部；逗号/分号/换行分隔。
    /// </summary>
    public static bool IsAllowed(string endpoint, string? allowList)
    {
        if (string.IsNullOrWhiteSpace(allowList))
        {
            return true;
        }

        var ip = ExtractIp(endpoint);
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        var parts = allowList.Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(p => string.Equals(p.Trim(), ip, StringComparison.OrdinalIgnoreCase));
    }
}
