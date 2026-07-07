using System.Net.Http.Json;
using System.Text.Json;
using Briefcase.Components.Services;
using Briefcase.Domain.Entities;
using Microsoft.JSInterop;

namespace Briefcase.Web.Services;

/// <summary>
/// Blazor WASM implementation of IE2eeService.
/// Uses the browser's SubtleCrypto API via JS interop for all crypto operations
/// (AesGcm is not available on the browser platform in .NET).
///
/// Key derivation  : PBKDF2-SHA256 (600 000 iterations, 256-bit output) via SubtleCrypto
/// Encryption      : AES-256-GCM via SubtleCrypto
/// Wire format     : Content  = Base64(ciphertext ‖ 16-byte GCM tag)
///                   IV field = Base64(12-byte nonce)
/// </summary>
public sealed class WebE2eeService(HttpClient httpClient, IJSRuntime js) : IE2eeService
{
    private const string KdfAlgorithmName = "PBKDF2-SHA256";
    private const int KdfIterations = 600_000;
    private const string KdfHashAlgorithm = "SHA256";
    private const string StoredPassphraseLocalKey = "Briefcase_e2ee_passphrase_local";
    private const string StoredPassphraseSessionKey = "Briefcase_e2ee_passphrase_session";
    private const string RememberPassphraseKey = "Briefcase_e2ee_remember_passphrase";

    // Derived key is stored as Base64 in memory.  The raw bytes never leave JS.
    private string? _keyB64;

    public bool IsUnlocked => _keyB64 is not null;
    public void Lock()
    {
        _keyB64 = null;
        ClearStoredPassphraseSync();
    }

    public async Task<bool> GetRememberPassphraseAsync()
    {
        var value = await js.InvokeAsync<string?>("localStorage.getItem", RememberPassphraseKey);
        return bool.TryParse(value, out var remember) && remember;
    }

    public async Task SetRememberPassphraseAsync(bool remember)
    {
        await js.InvokeVoidAsync("localStorage.setItem", RememberPassphraseKey, remember ? "true" : "false");

        if (remember)
        {
            var passphrase = await js.InvokeAsync<string?>("sessionStorage.getItem", StoredPassphraseSessionKey);
            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                await js.InvokeVoidAsync("localStorage.setItem", StoredPassphraseLocalKey, passphrase);
                await js.InvokeVoidAsync("sessionStorage.removeItem", StoredPassphraseSessionKey);
            }
        }
        else
        {
            var passphrase = await js.InvokeAsync<string?>("localStorage.getItem", StoredPassphraseLocalKey);
            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                await js.InvokeVoidAsync("sessionStorage.setItem", StoredPassphraseSessionKey, passphrase);
                await js.InvokeVoidAsync("localStorage.removeItem", StoredPassphraseLocalKey);
            }
        }
    }

    public async Task<E2eeSettingsDto?> GetSettingsAsync()
    {
        var response = await httpClient.GetAsync("api/e2ee/settings");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<E2eeSettingsDto>(JsonOptions);
    }

    public async Task EnableAsync(string passphrase)
    {
        var saltB64 = await js.InvokeAsync<string>("BriefcaseE2ee.randomSalt");
        var keyB64 = await DeriveKeyAsync(passphrase, saltB64, KdfIterations, KdfHashAlgorithm);
        var verifier = await js.InvokeAsync<string>("BriefcaseE2ee.buildVerifier", keyB64);

        var request = new
        {
            kdfAlgorithm = KdfAlgorithmName,
            kdfSalt = saltB64,
            kdfParams = JsonSerializer.Serialize(new { iterations = KdfIterations, hashAlgorithm = KdfHashAlgorithm }),
            keyVerifier = verifier
        };

        (await httpClient.PostAsJsonAsync("api/e2ee/enable", request)).EnsureSuccessStatusCode();
        _keyB64 = keyB64;
        await PersistPassphraseAsync(passphrase);
    }

    public async Task DisableAsync()
    {
        (await httpClient.PostAsync("api/e2ee/disable", null)).EnsureSuccessStatusCode();
        _keyB64 = null;
        await ClearStoredPassphraseAsync();
    }

    public async Task ChangePassphraseAsync(string newPassphrase, IReadOnlyList<Message> encryptedMessages,
        Func<Guid, string, string?, Task> updateMessageCallback)
    {
        if (_keyB64 is null) throw new InvalidOperationException("Service must be unlocked first.");

        var newSaltB64 = await js.InvokeAsync<string>("BriefcaseE2ee.randomSalt");
        var newKeyB64 = await DeriveKeyAsync(newPassphrase, newSaltB64, KdfIterations, KdfHashAlgorithm);
        var newVerifier = await js.InvokeAsync<string>("BriefcaseE2ee.buildVerifier", newKeyB64);

        foreach (var m in encryptedMessages.Where(m => m.IsEncrypted && m.Content is not null && m.EncryptionIV is not null))
        {
            string plaintext;
            try { plaintext = await js.InvokeAsync<string>("BriefcaseE2ee.decrypt", _keyB64, m.Content!, m.EncryptionIV!); }
            catch { continue; }

            var result = await js.InvokeAsync<EncryptResult>("BriefcaseE2ee.encrypt", newKeyB64, plaintext);
            await updateMessageCallback(m.Id, result.Ciphertext, result.Iv);
        }

        var changeRequest = new
        {
            kdfAlgorithm = KdfAlgorithmName,
            kdfSalt = newSaltB64,
            kdfParams = JsonSerializer.Serialize(new { iterations = KdfIterations, hashAlgorithm = KdfHashAlgorithm }),
            keyVerifier = newVerifier
        };

        (await httpClient.PutAsJsonAsync("api/e2ee/change-passphrase", changeRequest)).EnsureSuccessStatusCode();
        _keyB64 = newKeyB64;
        await PersistPassphraseAsync(newPassphrase);
    }

    public async Task<bool> TryUnlockAsync(string passphrase)
    {
        var settings = await GetSettingsAsync();
        if (settings is null || !settings.IsEnabled || settings.KdfSalt is null || settings.KeyVerifier is null)
            return false;

        var (iterations, hashAlgorithm) = ParseKdfParams(settings.KdfParams);
        var keyB64 = await DeriveKeyAsync(passphrase, settings.KdfSalt, iterations, hashAlgorithm);
        var ok = await js.InvokeAsync<bool>("BriefcaseE2ee.verifyKey", keyB64, settings.KeyVerifier);
        if (!ok) return false;

        _keyB64 = keyB64;
        await PersistPassphraseAsync(passphrase);
        return true;
    }

    public async Task<bool> TryAutoUnlockAsync()
    {
        if (IsUnlocked)
            return true;

        var passphrase = await GetStoredPassphraseAsync();
        if (string.IsNullOrWhiteSpace(passphrase))
            return false;

        var ok = await TryUnlockAsync(passphrase);
        if (!ok)
            await ClearStoredPassphraseAsync();

        return ok;
    }

    // ── Async crypto (IE2eeService) ───────────────────────────────────────────

    public async Task<(string Ciphertext, string Iv)> EncryptAsync(string plaintext)
    {
        if (_keyB64 is null)
            await TryAutoUnlockAsync();

        if (_keyB64 is null) throw new InvalidOperationException("E2EE service is locked.");
        var result = await js.InvokeAsync<EncryptResult>("BriefcaseE2ee.encrypt", _keyB64, plaintext);
        return (result.Ciphertext, result.Iv);
    }

    public async Task<string> DecryptAsync(string ciphertext, string iv)
    {
        if (_keyB64 is null)
            await TryAutoUnlockAsync();

        if (_keyB64 is null) throw new InvalidOperationException("E2EE service is locked.");
        return await js.InvokeAsync<string>("BriefcaseE2ee.decrypt", _keyB64, ciphertext, iv);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> DeriveKeyAsync(string passphrase, string saltB64, int iterations, string hashAlgorithm)
    {
        var passphraseB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(passphrase));
        return await js.InvokeAsync<string>("BriefcaseE2ee.deriveKey", passphraseB64, saltB64, iterations, hashAlgorithm);
    }

    private static (int Iterations, string HashAlgorithm) ParseKdfParams(string? kdfParamsJson)
    {
        var iterations = KdfIterations;
        var hashAlgorithm = KdfHashAlgorithm;

        if (string.IsNullOrWhiteSpace(kdfParamsJson))
            return (iterations, hashAlgorithm);

        try
        {
            using var json = JsonDocument.Parse(kdfParamsJson);
            if (json.RootElement.TryGetProperty("iterations", out var iterationsProp)
                && iterationsProp.ValueKind == JsonValueKind.Number
                && iterationsProp.TryGetInt32(out var parsedIterations)
                && parsedIterations > 0)
            {
                iterations = parsedIterations;
            }

            if (json.RootElement.TryGetProperty("hashAlgorithm", out var hashProp)
                && hashProp.ValueKind == JsonValueKind.String)
            {
                var parsedHash = hashProp.GetString();
                if (!string.IsNullOrWhiteSpace(parsedHash))
                    hashAlgorithm = parsedHash;
            }
        }
        catch
        {
            // Fall back to defaults when stored KDF params are malformed.
        }

        return (iterations, hashAlgorithm);
    }

    private async Task PersistPassphraseAsync(string passphrase)
    {
        if (await GetRememberPassphraseAsync())
        {
            await js.InvokeVoidAsync("localStorage.setItem", StoredPassphraseLocalKey, passphrase);
            await js.InvokeVoidAsync("sessionStorage.removeItem", StoredPassphraseSessionKey);
        }
        else
        {
            await js.InvokeVoidAsync("sessionStorage.setItem", StoredPassphraseSessionKey, passphrase);
            await js.InvokeVoidAsync("localStorage.removeItem", StoredPassphraseLocalKey);
        }
    }

    private async Task ClearStoredPassphraseAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", StoredPassphraseLocalKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", StoredPassphraseSessionKey);
    }

    private void ClearStoredPassphraseSync()
    {
        if (js is IJSInProcessRuntime inProcess)
        {
            inProcess.InvokeVoid("localStorage.removeItem", StoredPassphraseLocalKey);
            inProcess.InvokeVoid("sessionStorage.removeItem", StoredPassphraseSessionKey);
        }
    }

    private async Task<string?> GetStoredPassphraseAsync()
    {
        var local = await js.InvokeAsync<string?>("localStorage.getItem", StoredPassphraseLocalKey);
        if (!string.IsNullOrWhiteSpace(local))
            return local;

        return await js.InvokeAsync<string?>("sessionStorage.getItem", StoredPassphraseSessionKey);
    }

    private sealed class EncryptResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("ciphertext")]
        public string Ciphertext { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("iv")]
        public string Iv { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
