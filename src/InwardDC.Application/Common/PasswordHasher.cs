using System.Security.Cryptography;

namespace InwardDC.Application.Common;

/// <summary>
/// PBKDF2 (Rfc2898) password hashing with a per-user random salt. Uses only the
/// .NET BCL — no paid or third party dependencies, fully offline.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static (string Hash, string Salt) Hash(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool Verify(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expected = Convert.FromBase64String(hash);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return false;
        return password.Any(char.IsLetter) && password.Any(char.IsDigit);
    }
}
