using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanMonitor.Sender;

internal sealed class SenderSettings
{
    /// <summary>运行时明文；不写进 json。</summary>
    [JsonIgnore]
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>DPAPI 保护后的主机地址（本机范围）。</summary>
    public string HostProtected { get; set; } = string.Empty;

    /// <summary>兼容旧版明文 Host 字段（仅反序列化用）。</summary>
    [JsonPropertyName("Host")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? HostLegacy { get; set; }

    public int Port { get; set; } = 19730;
    public int Fps { get; set; } = 5;
    public int Quality { get; set; } = 60;
    public int MaxEdge { get; set; } = 1280;
    public bool Streaming { get; set; } = true;
    public bool AutoReconnect { get; set; } = true;
    public bool ContinuousReconnect { get; set; } = true;
    public int MaxReconnectAttempts { get; set; } = 5;
    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int HotkeyModifiers { get; set; } = NativeHotkey.ModControl | NativeHotkey.ModShift | NativeHotkey.ModAlt;
    public int HotkeyVirtualKey { get; set; } = (int)Keys.M;

    public bool Configured { get; set; }

    [JsonIgnore]
    public bool HasPassword =>
        !string.IsNullOrEmpty(PasswordSalt) && !string.IsNullOrEmpty(PasswordHash);

    /// <summary>机器级配置目录（各 Windows 用户共用）。</summary>
    [JsonIgnore]
    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SF_link");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    [JsonIgnore]
    private static string PerUserConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SF_link");

    [JsonIgnore]
    private static string PerUserConfigPath => Path.Combine(PerUserConfigDirectory, "settings.json");

    [JsonIgnore]
    private static string LegacyConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "局域网监控发送端");

    [JsonIgnore]
    private static string LegacyConfigPath => Path.Combine(LegacyConfigDirectory, "settings.json");

    public static SenderSettings LoadOrDefault()
    {
        try
        {
            var path = ResolveConfigPath();
            if (path is null)
            {
                return new SenderSettings();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<SenderSettings>(json) ?? new SenderSettings();
            var hadLegacyPlain = !string.IsNullOrEmpty(settings.HostLegacy);
            var fromNonMachinePath = !string.Equals(path, ConfigPath, StringComparison.OrdinalIgnoreCase);
            settings.NormalizeAfterLoad(out var hostNeedsReprotect);

            if (fromNonMachinePath || hadLegacyPlain || hostNeedsReprotect ||
                (settings.Configured && string.IsNullOrEmpty(settings.HostProtected)))
            {
                try { settings.Save(); } catch { /* ignore migrate errors */ }
            }

            return settings;
        }
        catch
        {
            return new SenderSettings();
        }
    }

    private static string? ResolveConfigPath()
    {
        if (File.Exists(ConfigPath))
        {
            return ConfigPath;
        }

        if (File.Exists(PerUserConfigPath))
        {
            return PerUserConfigPath;
        }

        if (File.Exists(LegacyConfigPath))
        {
            return LegacyConfigPath;
        }

        return null;
    }

    public void NormalizeAfterLoad(out bool hostNeedsReprotect)
    {
        hostNeedsReprotect = false;
        if (!string.IsNullOrEmpty(HostProtected))
        {
            try
            {
                Host = ConfigProtector.Unprotect(HostProtected, out hostNeedsReprotect);
            }
            catch
            {
                // 无法解密时不落盘覆盖，避免误写坏机器级配置
                Host = "127.0.0.1";
            }
        }
        else if (!string.IsNullOrEmpty(HostLegacy))
        {
            Host = HostLegacy;
            hostNeedsReprotect = true;
        }

        HostLegacy = null;
    }

    public void Save()
    {
        HostProtected = ConfigProtector.Protect(Host);
        HostLegacy = null;
        EnsureConfigDirectory();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>
    /// 创建 %ProgramData%\SF_link，并尽量授予 Users 修改权限，
    /// 以便其它本机用户热键改配置后也能写回。
    /// </summary>
    private static void EnsureConfigDirectory()
    {
        var info = Directory.CreateDirectory(ConfigDirectory);

        try
        {
            var security = info.GetAccessControl();
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            security.AddAccessRule(new FileSystemAccessRule(
                users,
                FileSystemRights.Modify | FileSystemRights.Synchronize,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            info.SetAccessControl(security);
        }
        catch
        {
            /* 无权限改 ACL 时忽略；管理员首次保存通常可成功 */
        }
    }
}
