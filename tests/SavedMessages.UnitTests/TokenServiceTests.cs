using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using SavedMessages.ApiService.Services;

namespace SavedMessages.UnitTests;

[TestClass]
public sealed class TokenServiceTests
{
    private static TokenService CreateService(int accessTokenMinutes = 15, int refreshTokenDays = 365) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-test-key-that-is-at-least-32-bytes-long!",
                ["Jwt:Issuer"] = "SavedMessages",
                ["Jwt:Audience"] = "SavedMessages",
                ["Jwt:AccessTokenMinutes"] = accessTokenMinutes.ToString(),
                ["Jwt:RefreshTokenDays"] = refreshTokenDays.ToString(),
            })
            .Build());

    // ── GenerateAccessToken ──────────────────────────────────────────────────

    [TestMethod]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        var svc = CreateService();
        var (token, _) = svc.GenerateAccessToken(Guid.NewGuid(), "user@example.com");
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
    }

    [TestMethod]
    public void GenerateAccessToken_ExpiresAtIsInFuture()
    {
        var svc = CreateService();
        var (_, expiresAt) = svc.GenerateAccessToken(Guid.NewGuid(), "user@example.com");
        Assert.IsTrue(expiresAt > DateTime.UtcNow);
    }

    [TestMethod]
    public void GenerateAccessToken_ExpiresAtReflectsConfiguredMinutes()
    {
        var svc = CreateService(accessTokenMinutes: 30);
        var before = DateTime.UtcNow.AddMinutes(29);
        var (_, expiresAt) = svc.GenerateAccessToken(Guid.NewGuid(), "user@example.com");
        var after = DateTime.UtcNow.AddMinutes(31);
        Assert.IsTrue(expiresAt >= before && expiresAt <= after);
    }

    [TestMethod]
    public void GenerateAccessToken_TokenContainsSubjectClaim()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();
        var (token, _) = svc.GenerateAccessToken(userId, "user@example.com");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.AreEqual(userId.ToString(), jwt.Subject);
    }

    [TestMethod]
    public void GenerateAccessToken_TokenContainsEmailClaim()
    {
        var svc = CreateService();
        const string email = "user@example.com";
        var (token, _) = svc.GenerateAccessToken(Guid.NewGuid(), email);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.IsTrue(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email));
    }

    [TestMethod]
    public void GenerateAccessToken_TokenContainsJtiClaim()
    {
        var svc = CreateService();
        var (token, _) = svc.GenerateAccessToken(Guid.NewGuid(), "u@e.com");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.IsFalse(string.IsNullOrEmpty(jwt.Id));
    }

    [TestMethod]
    public void GenerateAccessToken_ProducesDifferentJtiOnEachCall()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();
        var (t1, _) = svc.GenerateAccessToken(userId, "u@e.com");
        var (t2, _) = svc.GenerateAccessToken(userId, "u@e.com");
        var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(t1);
        var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(t2);
        Assert.AreNotEqual(jwt1.Id, jwt2.Id);
    }

    // ── GenerateRefreshToken ─────────────────────────────────────────────────

    [TestMethod]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var svc = CreateService();
        Assert.IsFalse(string.IsNullOrWhiteSpace(svc.GenerateRefreshToken()));
    }

    [TestMethod]
    public void GenerateRefreshToken_IsValidBase64With64Bytes()
    {
        var svc = CreateService();
        var token = svc.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);
        Assert.AreEqual(64, bytes.Length);
    }

    [TestMethod]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var svc = CreateService();
        Assert.AreNotEqual(svc.GenerateRefreshToken(), svc.GenerateRefreshToken());
    }

    // ── RefreshTokenDays ─────────────────────────────────────────────────────

    [TestMethod]
    public void RefreshTokenDays_ReturnsConfiguredValue()
    {
        var svc = CreateService(refreshTokenDays: 90);
        Assert.AreEqual(90, svc.RefreshTokenDays);
    }

    [TestMethod]
    public void RefreshTokenDays_DefaultsTo365_WhenNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-test-key-that-is-at-least-32-bytes-long!",
                ["Jwt:Issuer"] = "SavedMessages",
                ["Jwt:Audience"] = "SavedMessages",
            })
            .Build();
        var svc = new TokenService(config);
        Assert.AreEqual(365, svc.RefreshTokenDays);
    }

    // ── GenerateDevicePairToken ──────────────────────────────────────────────

    [TestMethod]
    public void GenerateDevicePairToken_ReturnsNonEmptyToken()
    {
        var svc = CreateService();
        Assert.IsFalse(string.IsNullOrWhiteSpace(svc.GenerateDevicePairToken(Guid.NewGuid())));
    }

    [TestMethod]
    public void GenerateDevicePairToken_ContainsDevicePairPurposeClaim()
    {
        var svc = CreateService();
        var token = svc.GenerateDevicePairToken(Guid.NewGuid());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.IsTrue(jwt.Claims.Any(c => c.Type == "purpose" && c.Value == "device-pair"));
    }

    [TestMethod]
    public void GenerateDevicePairToken_ContainsSubjectClaim()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();
        var token = svc.GenerateDevicePairToken(userId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.AreEqual(userId.ToString(), jwt.Subject);
    }

    // ── ValidateDevicePairToken ──────────────────────────────────────────────

    [TestMethod]
    public void ValidateDevicePairToken_ReturnsPrincipal_ForValidToken()
    {
        var svc = CreateService();
        var token = svc.GenerateDevicePairToken(Guid.NewGuid());
        var principal = svc.ValidateDevicePairToken(token);
        Assert.IsNotNull(principal);
    }

    [TestMethod]
    public void ValidateDevicePairToken_ReturnsNull_ForGarbageInput()
    {
        var svc = CreateService();
        Assert.IsNull(svc.ValidateDevicePairToken("not.a.valid.jwt"));
    }

    [TestMethod]
    public void ValidateDevicePairToken_ReturnsNull_ForAccessToken()
    {
        // An access token lacks the "purpose: device-pair" claim
        var svc = CreateService();
        var (accessToken, _) = svc.GenerateAccessToken(Guid.NewGuid(), "u@e.com");
        Assert.IsNull(svc.ValidateDevicePairToken(accessToken));
    }

    [TestMethod]
    public void ValidateDevicePairToken_ReturnsNull_ForTokenSignedWithDifferentKey()
    {
        var svc1 = CreateService();
        var svc2 = new TokenService(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "completely-different-secret-key-that-is-also-long!",
                    ["Jwt:Issuer"] = "SavedMessages",
                    ["Jwt:Audience"] = "SavedMessages",
                })
                .Build());

        var token = svc1.GenerateDevicePairToken(Guid.NewGuid());
        Assert.IsNull(svc2.ValidateDevicePairToken(token));
    }
}
