namespace LanMonitor.Sender;

internal static class PasswordHasher
{
    public static void SetPassword(SenderSettings settings, string password)
    {
        LanMonitor.Core.PasswordHasher.Create(password, out var salt, out var hash);
        settings.PasswordSalt = salt;
        settings.PasswordHash = hash;
    }

    public static bool Verify(SenderSettings settings, string password) =>
        LanMonitor.Core.PasswordHasher.Verify(settings.PasswordSalt, settings.PasswordHash, password);
}
