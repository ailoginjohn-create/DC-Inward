using Xunit;
using InwardDC.Application.Common;

namespace InwardDC.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ProducesDistinctSalts_ForSamePassword()
    {
        var (h1, s1) = PasswordHasher.Hash("P@ssw0rd");
        var (h2, s2) = PasswordHasher.Hash("P@ssw0rd");

        Assert.NotEqual(s1, s2);
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var (hash, salt) = PasswordHasher.Hash("CorrectHorseBattery1");

        Assert.True(PasswordHasher.Verify("CorrectHorseBattery1", hash, salt));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var (hash, salt) = PasswordHasher.Hash("CorrectHorseBattery1");

        Assert.False(PasswordHasher.Verify("wrong-password", hash, salt));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForCorruptedHash()
    {
        var (hash, salt) = PasswordHasher.Hash("CorrectHorseBattery1");

        Assert.False(PasswordHasher.Verify("CorrectHorseBattery1", "!!not-base64!!", salt));
    }

    [Theory]
    [InlineData("abc123", true)]
    [InlineData("abcdef", false)]
    [InlineData("123456", false)]
    [InlineData("12345", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStrong_AppliesLetterDigitLengthPolicy(string? password, bool expected)
    {
        Assert.Equal(expected, PasswordHasher.IsStrong(password ?? string.Empty));
    }
}
