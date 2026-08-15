using System.Security.Cryptography;

namespace LanMonitor.Core;

/// <summary>PBKDF2 密码哈希，发送端/接收端共用。</summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static void Create(string password, out string saltBase64, out string hashBase64)
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
        saltBase64 = Convert.ToBase64String(salt);
        hashBase64 = Convert.ToBase64String(hash);
    }

    public static bool Verify(string saltBase64, string hashBase64, string password)
    {
        if (string.IsNullOrEmpty(saltBase64) ||
            string.IsNullOrEmpty(hashBase64) ||
            string.IsNullOrEmpty(password))
        {
            return false;
        }

        var salt = Convert.FromBase64String(saltBase64);
        var expected = Convert.FromBase64String(hashBase64);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
