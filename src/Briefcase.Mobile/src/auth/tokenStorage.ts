import * as SecureStore from 'expo-secure-store'

// Persistent token storage backed by the device secure enclave / keystore.
// SecureStore is async, so we mirror the tokens in memory for synchronous reads
// (e.g. building authenticated URLs). Call `tokenStorage.load()` once at startup.

const ACCESS_TOKEN_KEY = 'briefcase_access_token'
const REFRESH_TOKEN_KEY = 'briefcase_refresh_token'

let accessToken: string | null = null
let refreshToken: string | null = null

async function persist(key: string, value: string | null): Promise<void> {
    if (value === null) await SecureStore.deleteItemAsync(key)
    else await SecureStore.setItemAsync(key, value)
}

export const tokenStorage = {
    /** Loads tokens from secure storage into the in-memory cache. */
    async load(): Promise<void> {
        accessToken = await SecureStore.getItemAsync(ACCESS_TOKEN_KEY)
        refreshToken = await SecureStore.getItemAsync(REFRESH_TOKEN_KEY)
    },

    getAccessToken(): string | null {
        return accessToken
    },
    setAccessToken(token: string): void {
        accessToken = token
        void persist(ACCESS_TOKEN_KEY, token)
    },
    getRefreshToken(): string | null {
        return refreshToken
    },
    setRefreshToken(token: string): void {
        refreshToken = token
        void persist(REFRESH_TOKEN_KEY, token)
    },
    clear(): void {
        accessToken = null
        refreshToken = null
        void persist(ACCESS_TOKEN_KEY, null)
        void persist(REFRESH_TOKEN_KEY, null)
    },
}
