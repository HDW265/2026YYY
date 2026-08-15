using System.Security.Cryptography;
using System.Text;

namespace LanMonitor.Sender;

/// <summary>用当前 Windows 用户 DPAPI 保护敏感字段（防随手打开 json）。</summary>
internal static class ConfigProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SF_link.settings.v1");

    public static string Protect(string plain)
    {
        if (string.IsNullOrEmpty(plain))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(plain);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedBase64)
    {
        if (string.IsNullOrEmpty(protectedBase64))
        {
            return string.Empty;
        }

        var protectedBytes = Convert.FromBase64String(protectedBase64);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
