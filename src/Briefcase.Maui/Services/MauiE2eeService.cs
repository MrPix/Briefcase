using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Briefcase.Components.Services;
using Briefcase.Domain.Entities;

namespace Briefcase.Maui.Services;

/// <summary>
/// MAUI implementation of IE2eeService.
/// Uses System.Security.Cryptography (PBKDF2-SHA256 + AES-256-GCM).
/// </summary>
public sealed class MauiE2eeService(HttpClient httpClient) : IE2eeService
{
    private const string KdfAlgorithmName = "PBKDF2-SHA256";
    private const int KdfIterations = 600_000;
    private const int KeyBytes = 32;
    private const int SaltBytes = 16;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const string VerifierSentinel = "briefcase-e2ee-check";

    private byte[]? _key;

    public bool IsUnlocked => _key is not null;
    public void Lock() => _key = null;

    public async Task<E2eeSettingsDto?> GetSettingsAsync()
    {
        var response = await httpClient.GetAsync("api/e2ee/settings");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<E2eeSettingsDto>(JsonOptions);
    }

    public async Task EnableAsync(string passphrase)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = DeriveKey(passphrase, salt);

        var request = new
        {
            kdfAlgorithm = KdfAlgorithmName,
            kdfSalt = Convert.ToBase64String(salt),
            kdfParams = JsonSerializer.Serialize(new { iterations = KdfIterations, hashAlgorithm = "SHA256" }),
            keyVerifier = BuildVerifier(key)
        };

        (await httpClient.PostAsJsonAsync("api/e2ee/enable", request)).EnsureSuccessStatusCode();
        _key = key;
    }

    public async Task DisableAsync()
    {
        (await httpClient.PostAsync("api/e2ee/disable", null)).EnsureSuccessStatusCode();
        _key = null;
    }

    public async Task ChangePassphraseAsync(string newPassphrase, IReadOnlyList<Message> encryptedMessages,
        Func<Guid, string, string?, Task> updateMessageCallback)
    {
        if (_key is null) throw new InvalidOperationException("Service must be unlocked first.");

        var newSalt = RandomNumberGenerator.GetBytes(SaltBytes);
        var newKey = DeriveKey(newPassphrase, newSalt);

        foreach (var m in encryptedMessages.Where(m => m.IsEncrypted && m.Content is not null && m.EncryptionIV is not null))
        {
            string plaintext;
            try { plaintext = DecryptWithKey(m.Content!, m.EncryptionIV!, _key); }
            catch { continue; }

            var (cipher, iv) = EncryptWithKey(plaintext, newKey);
            await updateMessageCallback(m.Id, cipher, iv);
        }

        var changeRequest = new
        {
            kdfAlgorithm = KdfAlgorithmName,
            kdfSalt = Convert.ToBase64String(newSalt),
            kdfParams = JsonSerializer.Serialize(new { iterations = KdfIterations, hashAlgorithm = "SHA256" }),
            keyVerifier = BuildVerifier(newKey)
        };

        (await httpClient.PutAsJsonAsync("api/e2ee/change-passphrase", changeRequest)).EnsureSuccessStatusCode();
        _key = newKey;
    }

    public async Task<bool> TryUnlockAsync(string passphrase)
    {
        var settings = await GetSettingsAsync();
        if (settings is null || !settings.IsEnabled || settings.KdfSalt is null || settings.KeyVerifier is null)
            return false;

        var key = DeriveKey(passphrase, Convert.FromBase64String(settings.KdfSalt));
        if (!VerifyKey(key, settings.KeyVerifier)) return false;

        _key = key;
        return true;
    }

    public Task<(string Ciphertext, string Iv)> EncryptAsync(string plaintext)
    {
        if (_key is null) throw new InvalidOperationException("E2EE service is locked.");
        return Task.FromResult(EncryptWithKey(plaintext, _key));
    }

    public Task<string> DecryptAsync(string ciphertext, string iv)
    {
        if (_key is null) throw new InvalidOperationException("E2EE service is locked.");
        return Task.FromResult(DecryptWithKey(ciphertext, iv, _key));
    }

    // ── Crypto ────────────────────────────────────────────────────────────────

    private static byte[] DeriveKey(string passphrase, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            KdfIterations,
            HashAlgorithmName.SHA256,
            KeyBytes);

    private static string BuildVerifier(byte[] key)
    {
        var plaintext = Encoding.UTF8.GetBytes(VerifierSentinel);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var tag = new byte[TagBytes];
        var cipher = new byte[plaintext.Length];

        using var aes = new AesGcm(key, TagBytes);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        var combined = new byte[NonceBytes + cipher.Length + TagBytes];
        nonce.CopyTo(combined, 0);
        cipher.CopyTo(combined, NonceBytes);
        tag.CopyTo(combined, NonceBytes + cipher.Length);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyKey(byte[] key, string verifierBase64)
    {
        try
        {
            var combined = Convert.FromBase64String(verifierBase64);
            var nonce = combined[..NonceBytes];
            var tag = combined[^TagBytes..];
            var cipher = combined[NonceBytes..^TagBytes];
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(key, TagBytes);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain) == VerifierSentinel;
        }
        catch { return false; }
    }

    private static (string Ciphertext, string Iv) EncryptWithKey(string plaintext, byte[] key)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var tag = new byte[TagBytes];
        var cipher = new byte[bytes.Length];

        using var aes = new AesGcm(key, TagBytes);
        aes.Encrypt(nonce, bytes, cipher, tag);

        var combined = new byte[cipher.Length + TagBytes];
        cipher.CopyTo(combined, 0);
        tag.CopyTo(combined, cipher.Length);
        return (Convert.ToBase64String(combined), Convert.ToBase64String(nonce));
    }

    private static string DecryptWithKey(string ciphertextBase64, string ivBase64, byte[] key)
    {
        var combined = Convert.FromBase64String(ciphertextBase64);
        var cipher = combined[..^TagBytes];
        var tag = combined[^TagBytes..];
        var nonce = Convert.FromBase64String(ivBase64);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagBytes);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
