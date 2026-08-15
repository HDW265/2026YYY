using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanMonitor.Sender;

internal sealed class SenderSettings
{
    public string Host { get; set; } = "127.0.0.1";
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

    /// <summary>Win32 MOD_* 组合：1=Alt 2=Ctrl 4=Shift 8=Win</summary>
    public int HotkeyModifiers { get; set; } = NativeHotkey.ModControl | NativeHotkey.ModShift | NativeHotkey.ModAlt;

    /// <summary>虚拟键码，默认 M</summary>
    public int HotkeyVirtualKey { get; set; } = (int)Keys.M;

    public bool Configured { get; set; }

    [JsonIgnore]
    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "局域网监控发送端");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    public static SenderSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return new SenderSettings();
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<SenderSettings>(json) ?? new SenderSettings();
        }
        catch
        {
            return new SenderSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public bool HasPassword =>
        !string.IsNullOrEmpty(PasswordSalt) && !string.IsNullOrEmpty(PasswordHash);
}
