using SavedMessages.Domain.Entities;

namespace SavedMessages.UnitTests;

[TestClass]
public sealed class RefreshTokenTests
{
    [TestMethod]
    public void IsRevoked_ReturnsFalse_WhenRevokedAtIsNull()
    {
        var token = new RefreshToken { RevokedAt = null };
        Assert.IsFalse(token.IsRevoked);
    }

    [TestMethod]
    public void IsRevoked_ReturnsTrue_WhenRevokedAtIsSet()
    {
        var token = new RefreshToken { RevokedAt = DateTime.UtcNow.AddDays(-1) };
        Assert.IsTrue(token.IsRevoked);
    }

    [TestMethod]
    public void IsExpired_ReturnsFalse_WhenExpiresAtIsInFuture()
    {
        var token = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddDays(1) };
        Assert.IsFalse(token.IsExpired);
    }

    [TestMethod]
    public void IsExpired_ReturnsTrue_WhenExpiresAtIsInPast()
    {
        var token = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        Assert.IsTrue(token.IsExpired);
    }

    [TestMethod]
    public void IsActive_ReturnsTrue_WhenNotRevokedAndNotExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null
        };
        Assert.IsTrue(token.IsActive);
    }

    [TestMethod]
    public void IsActive_ReturnsFalse_WhenRevoked()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };
        Assert.IsFalse(token.IsActive);
    }

    [TestMethod]
    public void IsActive_ReturnsFalse_WhenExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };
        Assert.IsFalse(token.IsActive);
    }

    [TestMethod]
    public void IsActive_ReturnsFalse_WhenBothExpiredAndRevoked()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow.AddDays(-2)
        };
        Assert.IsFalse(token.IsActive);
    }
}
