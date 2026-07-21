// End-to-end encryption — TypeScript port of the Blazor WebE2eeService + e2ee.js.
// Wire format is IDENTICAL so data encrypted by any Briefcase client interops:
//   Key derivation : PBKDF2-SHA256 (600 000 iterations, 256-bit) over the UTF-8
//                    bytes of the passphrase.
//   Encryption     : AES-256-GCM.
//   Content field  : Base64(ciphertext ‖ 16-byte GCM tag).
//   IV field       : Base64(12-byte nonce).
//   Verifier       : Base64(nonce ‖ ciphertext ‖ tag) of the sentinel string.

import { api } from '../lib/apiClient'
import type { E2eeSettings, Message } from '../types'

const KDF_ALGORITHM_NAME = 'PBKDF2-SHA256'
const KDF_ITERATIONS = 600_000
const KDF_HASH_ALGORITHM = 'SHA256'
const SENTINEL = 'briefcase-e2ee-check'

const STORED_PASSPHRASE_LOCAL_KEY = 'Briefcase_e2ee_passphrase_local'
const STORED_PASSPHRASE_SESSION_KEY = 'Briefcase_e2ee_passphrase_session'
const REMEMBER_PASSPHRASE_KEY = 'Briefcase_e2ee_remember_passphrase'

// ── Base64 helpers ──────────────────────────────────────────────────────────
function b64ToBytes(b64: string): Uint8Array<ArrayBuffer> {
    const binary = atob(b64)
    const bytes = new Uint8Array(binary.length)
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
    return bytes
}

/** UTF-8 encodes into an ArrayBuffer-backed Uint8Array (WebCrypto BufferSource). */
function utf8(text: string): Uint8Array<ArrayBuffer> {
    return new Uint8Array(new TextEncoder().encode(text))
}

function bytesToB64(bytes: Uint8Array): string {
    let binary = ''
    for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i])
    return btoa(binary)
}

function normalizeHash(hash: string | undefined): string {
    const n = String(hash ?? '').trim().toUpperCase()
    if (n === 'SHA384' || n === 'SHA-384') return 'SHA-384'
    if (n === 'SHA512' || n === 'SHA-512') return 'SHA-512'
    if (n === 'SHA1' || n === 'SHA-1') return 'SHA-1'
    return 'SHA-256'
}

async function deriveKeyBytes(
    passphrase: string,
    saltB64: string,
    iterations: number,
    hashAlgorithm: string,
): Promise<Uint8Array<ArrayBuffer>> {
    const passphraseBytes = utf8(passphrase)
    const salt = b64ToBytes(saltB64)
    const keyMaterial = await crypto.subtle.importKey('raw', passphraseBytes, 'PBKDF2', false, [
        'deriveBits',
    ])
    const keyBits = await crypto.subtle.deriveBits(
        { name: 'PBKDF2', salt, iterations, hash: normalizeHash(hashAlgorithm) },
        keyMaterial,
        256,
    )
    return new Uint8Array(keyBits)
}

function importAesKey(keyBytes: Uint8Array<ArrayBuffer>): Promise<CryptoKey> {
    return crypto.subtle.importKey('raw', keyBytes, 'AES-GCM', false, ['encrypt', 'decrypt'])
}

function randomSaltB64(): string {
    return bytesToB64(crypto.getRandomValues(new Uint8Array(16)))
}

function parseKdfParams(json: string | null | undefined): { iterations: number; hashAlgorithm: string } {
    if (json) {
        try {
            const parsed = JSON.parse(json)
            return {
                iterations: Number(parsed.iterations) || KDF_ITERATIONS,
                hashAlgorithm: parsed.hashAlgorithm ?? KDF_HASH_ALGORITHM,
            }
        } catch {
            /* fall through to defaults */
        }
    }
    return { iterations: KDF_ITERATIONS, hashAlgorithm: KDF_HASH_ALGORITHM }
}

/**
 * Stateful E2EE service. Holds the derived key in memory only; the raw key and
 * passphrase never reach the server.
 */
class E2eeService {
    private keyBytes: Uint8Array<ArrayBuffer> | null = null

    get isUnlocked(): boolean {
        return this.keyBytes !== null
    }

    lock(): void {
        this.keyBytes = null
        this.clearStoredPassphrase()
    }

    async getSettings(): Promise<E2eeSettings | null> {
        try {
            return await api.get<E2eeSettings>('api/e2ee/settings')
        } catch {
            return null
        }
    }

    async enable(passphrase: string): Promise<void> {
        const saltB64 = randomSaltB64()
        const keyBytes = await deriveKeyBytes(passphrase, saltB64, KDF_ITERATIONS, KDF_HASH_ALGORITHM)
        const verifier = await this.buildVerifier(keyBytes)

        await api.post('api/e2ee/enable', {
            kdfAlgorithm: KDF_ALGORITHM_NAME,
            kdfSalt: saltB64,
            kdfParams: JSON.stringify({ iterations: KDF_ITERATIONS, hashAlgorithm: KDF_HASH_ALGORITHM }),
            keyVerifier: verifier,
        })
        this.keyBytes = keyBytes
        await this.persistPassphrase(passphrase)
    }

    async disable(): Promise<void> {
        await api.post('api/e2ee/disable')
        this.keyBytes = null
        this.clearStoredPassphrase()
    }

    async changePassphrase(
        newPassphrase: string,
        encryptedMessages: Message[],
        updateMessage: (id: string, ciphertext: string, iv: string) => Promise<void>,
    ): Promise<void> {
        if (!this.keyBytes) throw new Error('Service must be unlocked first.')
        const oldKey = this.keyBytes

        const newSaltB64 = randomSaltB64()
        const newKeyBytes = await deriveKeyBytes(newPassphrase, newSaltB64, KDF_ITERATIONS, KDF_HASH_ALGORITHM)
        const newVerifier = await this.buildVerifier(newKeyBytes)

        for (const m of encryptedMessages) {
            if (!m.isEncrypted || m.content === null || m.encryptionIV === null) continue
            let plaintext: string
            try {
                plaintext = await this.decryptWith(oldKey, m.content, m.encryptionIV)
            } catch {
                continue
            }
            const result = await this.encryptWith(newKeyBytes, plaintext)
            await updateMessage(m.id, result.ciphertext, result.iv)
        }

        await api.put('api/e2ee/change-passphrase', {
            kdfAlgorithm: KDF_ALGORITHM_NAME,
            kdfSalt: newSaltB64,
            kdfParams: JSON.stringify({ iterations: KDF_ITERATIONS, hashAlgorithm: KDF_HASH_ALGORITHM }),
            keyVerifier: newVerifier,
        })
        this.keyBytes = newKeyBytes
        await this.persistPassphrase(newPassphrase)
    }

    async tryUnlock(passphrase: string): Promise<boolean> {
        const settings = await this.getSettings()
        if (!settings?.isEnabled || !settings.kdfSalt || !settings.keyVerifier) return false

        const { iterations, hashAlgorithm } = parseKdfParams(settings.kdfParams)
        const keyBytes = await deriveKeyBytes(passphrase, settings.kdfSalt, iterations, hashAlgorithm)
        const ok = await this.verifyKey(keyBytes, settings.keyVerifier)
        if (!ok) return false

        this.keyBytes = keyBytes
        await this.persistPassphrase(passphrase)
        return true
    }

    async tryAutoUnlock(): Promise<boolean> {
        if (this.isUnlocked) return true
        const passphrase = this.getStoredPassphrase()
        if (!passphrase) return false
        const ok = await this.tryUnlock(passphrase)
        if (!ok) this.clearStoredPassphrase()
        return ok
    }

    async encrypt(plaintext: string): Promise<{ ciphertext: string; iv: string }> {
        if (!this.keyBytes) throw new Error('Service is locked.')
        return this.encryptWith(this.keyBytes, plaintext)
    }

    async decrypt(ciphertext: string, iv: string): Promise<string> {
        if (!this.keyBytes) throw new Error('Service is locked.')
        return this.decryptWith(this.keyBytes, ciphertext, iv)
    }

    // ── Remember-passphrase persistence ───────────────────────────────────────
    getRememberPassphrase(): boolean {
        return localStorage.getItem(REMEMBER_PASSPHRASE_KEY) === 'true'
    }

    setRememberPassphrase(remember: boolean): void {
        localStorage.setItem(REMEMBER_PASSPHRASE_KEY, remember ? 'true' : 'false')
        if (remember) {
            const p = sessionStorage.getItem(STORED_PASSPHRASE_SESSION_KEY)
            if (p) {
                localStorage.setItem(STORED_PASSPHRASE_LOCAL_KEY, p)
                sessionStorage.removeItem(STORED_PASSPHRASE_SESSION_KEY)
            }
        } else {
            const p = localStorage.getItem(STORED_PASSPHRASE_LOCAL_KEY)
            if (p) {
                sessionStorage.setItem(STORED_PASSPHRASE_SESSION_KEY, p)
                localStorage.removeItem(STORED_PASSPHRASE_LOCAL_KEY)
            }
        }
    }

    // ── Internal crypto primitives ────────────────────────────────────────────
    private async encryptWith(
        keyBytes: Uint8Array<ArrayBuffer>,
        plaintext: string,
    ): Promise<{ ciphertext: string; iv: string }> {
        const key = await importAesKey(keyBytes)
        const iv = crypto.getRandomValues(new Uint8Array(12))
        const data = utf8(plaintext)
        const ctWithTag = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, key, data)
        return { ciphertext: bytesToB64(new Uint8Array(ctWithTag)), iv: bytesToB64(iv) }
    }

    private async decryptWith(keyBytes: Uint8Array<ArrayBuffer>, ciphertextB64: string, ivB64: string): Promise<string> {
        const key = await importAesKey(keyBytes)
        const ciphertext = b64ToBytes(ciphertextB64)
        const iv = b64ToBytes(ivB64)
        const plainBytes = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, key, ciphertext)
        return new TextDecoder().decode(plainBytes)
    }

    private async buildVerifier(keyBytes: Uint8Array<ArrayBuffer>): Promise<string> {
        const key = await importAesKey(keyBytes)
        const sentinel = utf8(SENTINEL)
        const iv = crypto.getRandomValues(new Uint8Array(12))
        const ctWithTag = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, key, sentinel)
        const combined = new Uint8Array(12 + ctWithTag.byteLength)
        combined.set(iv, 0)
        combined.set(new Uint8Array(ctWithTag), 12)
        return bytesToB64(combined)
    }

    private async verifyKey(keyBytes: Uint8Array<ArrayBuffer>, verifierB64: string): Promise<boolean> {
        try {
            const key = await importAesKey(keyBytes)
            const combined = b64ToBytes(verifierB64)
            const iv = combined.slice(0, 12)
            const ctWithTag = combined.slice(12)
            const plainBytes = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, key, ctWithTag)
            return new TextDecoder().decode(plainBytes) === SENTINEL
        } catch {
            return false
        }
    }

    // ── Passphrase persistence ────────────────────────────────────────────────
    private getStoredPassphrase(): string | null {
        return (
            localStorage.getItem(STORED_PASSPHRASE_LOCAL_KEY) ??
            sessionStorage.getItem(STORED_PASSPHRASE_SESSION_KEY)
        )
    }

    private async persistPassphrase(passphrase: string): Promise<void> {
        if (this.getRememberPassphrase()) {
            localStorage.setItem(STORED_PASSPHRASE_LOCAL_KEY, passphrase)
        } else {
            sessionStorage.setItem(STORED_PASSPHRASE_SESSION_KEY, passphrase)
        }
    }

    private clearStoredPassphrase(): void {
        localStorage.removeItem(STORED_PASSPHRASE_LOCAL_KEY)
        sessionStorage.removeItem(STORED_PASSPHRASE_SESSION_KEY)
    }
}

export const e2eeService = new E2eeService()
export type { E2eeService }
