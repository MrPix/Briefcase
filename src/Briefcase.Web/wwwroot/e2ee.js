// E2EE crypto helpers using the browser's SubtleCrypto API.
// All byte arrays are transferred as Base64 strings across the JS/C# boundary.

window.BriefcaseE2ee = {

    // Derives a 256-bit AES-GCM key from a passphrase using PBKDF2-SHA256.
    // Returns the raw key bytes as Base64.
    async deriveKey(passphraseB64, saltB64, iterations) {
        const passphraseBytes = b64ToBytes(passphraseB64);
        const salt = b64ToBytes(saltB64);

        const keyMaterial = await crypto.subtle.importKey(
            "raw", passphraseBytes, "PBKDF2", false, ["deriveBits"]);

        const keyBits = await crypto.subtle.deriveBits(
            { name: "PBKDF2", salt, iterations, hash: "SHA-256" },
            keyMaterial, 256);

        return bytesToB64(new Uint8Array(keyBits));
    },

    // Encrypts UTF-8 plaintext with AES-256-GCM.
    // Returns { ciphertext: Base64(ciphertext+tag), iv: Base64(nonce) }.
    async encrypt(keyB64, plaintext) {
        const keyBytes = b64ToBytes(keyB64);
        const key = await importAesKey(keyBytes);
        const iv = crypto.getRandomValues(new Uint8Array(12));
        const data = new TextEncoder().encode(plaintext);

        const ciphertextWithTag = await crypto.subtle.encrypt(
            { name: "AES-GCM", iv }, key, data);

        return {
            ciphertext: bytesToB64(new Uint8Array(ciphertextWithTag)),
            iv: bytesToB64(iv)
        };
    },

    // Decrypts AES-256-GCM ciphertext. ciphertextB64 = Base64(ciphertext+tag).
    // Returns plaintext string or throws on tag mismatch.
    async decrypt(keyB64, ciphertextB64, ivB64) {
        const keyBytes = b64ToBytes(keyB64);
        const key = await importAesKey(keyBytes);
        const ciphertext = b64ToBytes(ciphertextB64);
        const iv = b64ToBytes(ivB64);

        const plainBytes = await crypto.subtle.decrypt(
            { name: "AES-GCM", iv }, key, ciphertext);

        return new TextDecoder().decode(plainBytes);
    },

    // Builds a self-contained verifier: Base64(nonce + ciphertext + tag)
    // of the fixed sentinel string, used to verify a derived key is correct.
    async buildVerifier(keyB64) {
        const keyBytes = b64ToBytes(keyB64);
        const key = await importAesKey(keyBytes);
        const sentinel = new TextEncoder().encode("briefcase-e2ee-check");
        const iv = crypto.getRandomValues(new Uint8Array(12));

        const ciphertextWithTag = await crypto.subtle.encrypt(
            { name: "AES-GCM", iv }, key, sentinel);

        const combined = new Uint8Array(12 + ciphertextWithTag.byteLength);
        combined.set(iv, 0);
        combined.set(new Uint8Array(ciphertextWithTag), 12);
        return bytesToB64(combined);
    },

    // Returns true if the key can decrypt the verifier and the result equals the sentinel.
    async verifyKey(keyB64, verifierB64) {
        try {
            const keyBytes = b64ToBytes(keyB64);
            const key = await importAesKey(keyBytes);
            const combined = b64ToBytes(verifierB64);
            const iv = combined.slice(0, 12);
            const ciphertextWithTag = combined.slice(12);

            const plainBytes = await crypto.subtle.decrypt(
                { name: "AES-GCM", iv }, key, ciphertextWithTag);

            return new TextDecoder().decode(plainBytes) === "briefcase-e2ee-check";
        } catch {
            return false;
        }
    },

    // Generates a random salt and returns it as Base64.
    randomSalt() {
        return bytesToB64(crypto.getRandomValues(new Uint8Array(16)));
    }
};

async function importAesKey(keyBytes) {
    return crypto.subtle.importKey("raw", keyBytes, "AES-GCM", false, ["encrypt", "decrypt"]);
}

function b64ToBytes(b64) {
    const binary = atob(b64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}

function bytesToB64(bytes) {
    let binary = "";
    for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
    return btoa(binary);
}
