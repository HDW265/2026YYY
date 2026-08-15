using System.Security.Cryptography;

namespace LanMonitor.Sender;

internal static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static void SetPassword(SenderSettings settings, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("密码不能为空。", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        settings.PasswordSalt = Convert.ToBase64String(salt);
        settings.PasswordHash = Convert.ToBase64String(hash);
    }

    public static bool Verify(SenderSettings settings, string password)
    {
        if (!settings.HasPassword || string.IsNullOrEmpty(password))
        {
            return false;
        }

        var salt = Convert.FromBase64String(settings.PasswordSalt);
        var expected = Convert.FromBase64String(settings.PasswordHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
