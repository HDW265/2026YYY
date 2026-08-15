using System.Security.Cryptography;
using System.Text;

namespace LanMonitor.Sender;

/// <summary>
/// 用本机 DPAPI 保护敏感字段，便于同一台电脑上各 Windows 用户共用配置。
/// 仍兼容旧版 CurrentUser 密文（加载后会重加密为本机范围）。
/// </summary>
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
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>
    /// 解密 Host。优先 LocalMachine；失败再试 CurrentUser（旧配置迁移）。
    /// <paramref name="usedLegacyCurrentUser"/> 为 true 时调用方应重新 Save。
    /// </summary>
    public static string Unprotect(string protectedBase64, out bool usedLegacyCurrentUser)
    {
        usedLegacyCurrentUser = false;
        if (string.IsNullOrEmpty(protectedBase64))
        {
            return string.Empty;
        }

        var protectedBytes = Convert.FromBase64String(protectedBase64);

        try
        {
            var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            usedLegacyCurrentUser = true;
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
