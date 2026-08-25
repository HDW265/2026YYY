using System.Text.Json;

namespace LanMonitor.Receiver;

internal sealed class ReceiverSettings
{
    public string SaveDirectory { get; set; } = string.Empty;
    public bool LogExpanded { get; set; } = true;
    public int Port { get; set; } = 19730;
    public double IntervalSeconds { get; set; } = 1.0;
    public int Quality { get; set; } = 60;
    public bool PreviewOn { get; set; } = true;
    public bool SaveOn { get; set; } = true;

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LanMonitor.Receiver");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    public static ReceiverSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return CreateDefault();
            }

            var json = File.ReadAllText(ConfigPath);
            var settings = JsonSerializer.Deserialize<ReceiverSettings>(json) ?? CreateDefault();
            settings.Normalize();
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
                "局域网监控");
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
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
