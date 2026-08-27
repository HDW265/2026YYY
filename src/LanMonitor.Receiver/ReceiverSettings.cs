using System.Text.Json;
using System.Text.Json.Serialization;
using LanMonitor.Core;

namespace LanMonitor.Receiver;

public sealed class ReceiverSettings
{
    public string SaveDirectory { get; set; } = string.Empty;
    public bool LogExpanded { get; set; } = true;
    public int Port { get; set; } = 19730;
    public double IntervalSeconds { get; set; } = 1.0;
    public int Quality { get; set; } = 60;
    public bool PreviewOn { get; set; } = true;
    public bool SaveOn { get; set; } = true;
    public bool AllowAll { get; set; } = true;
    public List<string> KnownIps { get; set; } = new();
    public List<string> AllowedIps { get; set; } = new();

    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int HotkeyModifiers { get; set; } = NativeHotkey.ModControl | NativeHotkey.ModShift | NativeHotkey.ModAlt;
    public int HotkeyVirtualKey { get; set; } = (int)Keys.V;

    [JsonIgnore]
    public bool HasPassword =>
        !string.IsNullOrEmpty(PasswordSalt) && !string.IsNullOrEmpty(PasswordHash);

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SF_view");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    private static string LegacyConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LanMonitor.Receiver");

    private static string LegacyConfigPath => Path.Combine(LegacyConfigDirectory, "settings.json");

    public static ReceiverSettings LoadOrDefault()
    {
        try
        {
            var path = File.Exists(ConfigPath)
                ? ConfigPath
                : File.Exists(LegacyConfigPath) ? LegacyConfigPath : null;

            if (path is null)
            {
                return CreateDefault();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<ReceiverSettings>(json) ?? CreateDefault();
            settings.Normalize();

            if (!string.Equals(path, ConfigPath, StringComparison.OrdinalIgnoreCase))
            {
                try { settings.Save(); } catch { /* ignore migrate errors */ }
            }

            return settings;
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static ReceiverSettings CreateDefault()
    {
        var settings = new ReceiverSettings();
        settings.Normalize();
        return settings;
    }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(SaveDirectory))
        {
            SaveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "SF_view");
        }
        else if (SaveDirectory.EndsWith(Path.DirectorySeparatorChar + "局域网监控", StringComparison.OrdinalIgnoreCase)
                 || SaveDirectory.EndsWith("/局域网监控", StringComparison.OrdinalIgnoreCase)
                 || SaveDirectory.EndsWith("\\局域网监控", StringComparison.OrdinalIgnoreCase))
        {
            // keep user's existing remembered path; do not force rename
        }

        if (Port < 1 || Port > 65535)
        {
            Port = 19730;
        }

        if (IntervalSeconds < 0.2 || IntervalSeconds > 60)
        {
            IntervalSeconds = 1.0;
        }

        if (Quality < 1 || Quality > 100)
        {
            Quality = 60;
        }

        if (HotkeyModifiers == 0)
        {
            HotkeyModifiers = NativeHotkey.ModControl | NativeHotkey.ModShift | NativeHotkey.ModAlt;
        }

        if (HotkeyVirtualKey == 0)
        {
            HotkeyVirtualKey = (int)Keys.V;
        }

        KnownIps = KnownIps
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        AllowedIps = AllowedIps
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public AllowIpPolicySnapshot ToAllowPolicySnapshot() =>
        new(AllowAll, KnownIps, AllowedIps);

    public void ApplyAllowPolicySnapshot(AllowIpPolicySnapshot snapshot)
    {
        AllowAll = snapshot.AllowAll;
        KnownIps = snapshot.KnownIps.ToList();
        AllowedIps = snapshot.AllowedIps.ToList();
        Normalize();
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
