using System.Text.Json;
using System.Text.Json.Serialization;
using LanMonitor.Core;

namespace LanMonitor.Receiver;

internal sealed class ReceiverSettings
{
    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasPassword =>
        !string.IsNullOrEmpty(PasswordSalt) && !string.IsNullOrEmpty(PasswordHash);

    [JsonIgnore]
    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SF_view");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    public static ReceiverSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return new ReceiverSettings();
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<ReceiverSettings>(json) ?? new ReceiverSettings();
        }
        catch
        {
            return new ReceiverSettings();
        }
    }

    public void SetPassword(string password)
    {
        PasswordHasher.Create(password, out var salt, out var hash);
        PasswordSalt = salt;
        PasswordHash = hash;
    }

    public bool VerifyPassword(string password) =>
        PasswordHasher.Verify(PasswordSalt, PasswordHash, password);

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigPath, json);
    }
}
