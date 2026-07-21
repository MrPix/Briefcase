// Persistent token storage (localStorage). The React web client sends the
// refresh token in the request body (like the MAUI client) to avoid relying on
// cross-origin cookies during development.

const ACCESS_TOKEN_KEY = 'briefcase_access_token'
const REFRESH_TOKEN_KEY = 'briefcase_refresh_token'

export const tokenStorage = {
    getAccessToken(): string | null {
        return localStorage.getItem(ACCESS_TOKEN_KEY)
    },
    setAccessToken(token: string): void {
        localStorage.setItem(ACCESS_TOKEN_KEY, token)
    },
    getRefreshToken(): string | null {
        return localStorage.getItem(REFRESH_TOKEN_KEY)
    },
    setRefreshToken(token: string): void {
        localStorage.setItem(REFRESH_TOKEN_KEY, token)
    },
    clear(): void {
        localStorage.removeItem(ACCESS_TOKEN_KEY)
        localStorage.removeItem(REFRESH_TOKEN_KEY)
    },
}
