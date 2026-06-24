using Briefcase.Domain.Entities;

namespace Briefcase.Components.Services;

public record E2eeSettingsDto(
    bool IsEnabled,
    string? KdfAlgorithm,
    string? KdfSalt,
    string? KdfParams,
    string? KeyVerifier);

public interface IE2eeService
{
    /// <summary>True when a passphrase has been derived and the key is held in memory.</summary>
    bool IsUnlocked { get; }

    /// <summary>Fetches E2EE settings from the server (isEnabled, KDF params, key verifier).</summary>
    Task<E2eeSettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Enables E2EE for the current user. Derives a key from <paramref name="passphrase"/>,
    /// generates a random KDF salt and a key verifier, then stores the KDF artefacts on the server.
    /// The passphrase and derived key are never sent to the server.
    /// After success the service is unlocked with the new key.
    /// </summary>
    Task EnableAsync(string passphrase);

    /// <summary>
    /// Disables E2EE. The caller must have already re-uploaded all messages as plaintext.
    /// Clears KDF artefacts on the server and locks the service.
    /// </summary>
    Task DisableAsync();

    /// <summary>
    /// Re-keys all provided messages: decrypts with the current key, encrypts with a new key
    /// derived from <paramref name="newPassphrase"/>, then replaces the KDF artefacts on the server.
    /// The current service must be unlocked before calling.
    /// </summary>
    Task ChangePassphraseAsync(string newPassphrase, IReadOnlyList<Message> encryptedMessages,
        Func<Guid, string, string?, Task> updateMessageCallback);

    /// <summary>
    /// Attempts to derive the encryption key from <paramref name="passphrase"/> and verifies it
    /// against the stored key verifier. Returns true and unlocks the service if successful.
    /// </summary>
    Task<bool> TryUnlockAsync(string passphrase);

    /// <summary>Clears the in-memory key so messages can no longer be encrypted/decrypted.</summary>
    void Lock();

    /// <summary>Encrypts UTF-8 plaintext. Returns (base64 ciphertext, base64 IV).</summary>
    Task<(string Ciphertext, string Iv)> EncryptAsync(string plaintext);

    /// <summary>Decrypts AES-256-GCM ciphertext. Returns plaintext or throws on failure.</summary>
    Task<string> DecryptAsync(string ciphertext, string iv);
}
