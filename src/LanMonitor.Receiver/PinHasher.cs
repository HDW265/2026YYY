using System.Security.Cryptography;

namespace LanMonitor.Receiver;

internal static class PinHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static bool IsValidPinFormat(string? pin) =>
        !string.IsNullOrEmpty(pin) && pin.Length is >= 4 and <= 6 && pin.All(char.IsDigit);

    public static void SetPin(ReceiverSettings settings, string pin)
    {
        if (!IsValidPinFormat(pin))
        {
            throw new ArgumentException("验证码须为 4～6 位数字。", nameof(pin));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        settings.PasswordSalt = Convert.ToBase64String(salt);
        settings.PasswordHash = Convert.ToBase64String(hash);
    }

    public static bool Verify(ReceiverSettings settings, string pin)
    {
        if (!settings.HasPassword || string.IsNullOrEmpty(pin))
        {
            return false;
        }

        var salt = Convert.FromBase64String(settings.PasswordSalt);
        var expected = Convert.FromBase64String(settings.PasswordHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
