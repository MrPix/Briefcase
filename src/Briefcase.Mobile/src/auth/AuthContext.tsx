import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useRef,
    useState,
    type ReactNode,
} from 'react'
import * as Linking from 'expo-linking'
import * as WebBrowser from 'expo-web-browser'
import { apiFetch, onUnauthorized } from '../lib/apiClient'
import { API_BASE_URL } from '../lib/config'
import { messageStream } from '../realtime/messageStream'
import type { AuthResponse, ExternalAuthProvider } from '../types'
import { tokenStorage } from './tokenStorage'
import { deviceInfo } from './deviceInfo'

const EXTERNAL_PROVIDERS: ExternalAuthProvider[] = [{ key: 'Google', displayName: 'Google' }]

export class AuthException extends Error { }

interface AuthContextValue {
    isAuthenticated: boolean
    restoring: boolean
    externalProviders: ExternalAuthProvider[]
    login: (email: string, password: string) => Promise<void>
    register: (email: string, password: string, displayName: string) => Promise<void>
    logout: () => Promise<void>
    loginWithProvider: (provider: string) => Promise<void>
    completeExternalLogin: (
        accessToken: string,
        refreshToken: string,
        accessTokenExpiresAt: string,
    ) => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function tokenExpiryUtcMs(token: string): number | null {
    try {
        const payload = token.split('.')[1]
        if (!payload) return null
        const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
        const json = globalThis.atob(normalized)
        const data = JSON.parse(json) as { exp?: number }
        return typeof data.exp === 'number' ? data.exp * 1000 : null
    } catch {
        return null
    }
}

async function readProblemTitle(res: Response, fallback: string): Promise<string> {
    try {
        const data = await res.clone().json()
        return data?.title ?? data?.error ?? fallback
    } catch {
        return fallback
    }
}

function buildExternalLoginUrl(provider: string, clientRedirectUri: string): string {
    if (!EXTERNAL_PROVIDERS.some((p) => p.key.toLowerCase() === provider.toLowerCase())) {
        throw new AuthException(`Unsupported external auth provider: ${provider}.`)
    }
    const base = API_BASE_URL
    const encodedProvider = encodeURIComponent(provider)
    const callbackUri = `${base}/api/auth/oauth/${encodedProvider}/callback`
    const query =
        `redirect_uri=${encodeURIComponent(callbackUri)}` +
        `&client_redirect_uri=${encodeURIComponent(clientRedirectUri)}` +
        `&device_name=${encodeURIComponent(deviceInfo.deviceName)}` +
        `&device_platform=${encodeURIComponent(deviceInfo.platform)}` +
        `&installation_id=${encodeURIComponent(deviceInfo.installationId)}`
    return `${base}/api/auth/oauth/${encodedProvider}?${query}`
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [isAuthenticated, setIsAuthenticated] = useState(false)
    const [restoring, setRestoring] = useState(true)
    const expiresAtRef = useRef<number>(0)

    const storeTokens = useCallback((result: AuthResponse) => {
        tokenStorage.setAccessToken(result.accessToken)
        if (result.refreshToken) tokenStorage.setRefreshToken(result.refreshToken)
        expiresAtRef.current = new Date(result.accessTokenExpiresAt).getTime()
        setIsAuthenticated(true)
    }, [])

    const clearAuth = useCallback(() => {
        tokenStorage.clear()
        expiresAtRef.current = 0
        setIsAuthenticated(false)
    }, [])

    const refresh = useCallback(async (): Promise<boolean> => {
        const refreshToken = tokenStorage.getRefreshToken()
        if (!refreshToken) return false
        const res = await apiFetch('api/auth/refresh', {
            method: 'POST',
            body: { refreshToken },
            skipAuth: true,
        })
        if (!res.ok) return false
        storeTokens((await res.json()) as AuthResponse)
        return true
    }, [storeTokens])

    const login = useCallback(
        async (email: string, password: string) => {
            const res = await apiFetch('api/auth/login', {
                method: 'POST',
                skipAuth: true,
                body: {
                    email,
                    password,
                    deviceName: deviceInfo.deviceName,
                    devicePlatform: deviceInfo.platform,
                    installationId: deviceInfo.installationId,
                },
            })
            if (!res.ok) throw new AuthException(await readProblemTitle(res, 'Login failed.'))
            storeTokens((await res.json()) as AuthResponse)
        },
        [storeTokens],
    )

    const register = useCallback(
        async (email: string, password: string, displayName: string) => {
            const res = await apiFetch('api/auth/register', {
                method: 'POST',
                skipAuth: true,
                body: {
                    email,
                    password,
                    displayName,
                    deviceName: deviceInfo.deviceName,
                    devicePlatform: deviceInfo.platform,
                    installationId: deviceInfo.installationId,
                },
            })
            if (!res.ok) throw new AuthException(await readProblemTitle(res, 'Registration failed.'))
            storeTokens((await res.json()) as AuthResponse)
        },
        [storeTokens],
    )

    const logout = useCallback(async () => {
        try {
            await apiFetch('api/auth/logout', { method: 'POST' })
        } catch {
            /* best-effort server-side revocation */
        }
        await messageStream.stop().catch(() => { })
        clearAuth()
    }, [clearAuth])

    const completeExternalLogin = useCallback(
        (accessToken: string, refreshToken: string, accessTokenExpiresAt: string) => {
            storeTokens({ accessToken, refreshToken, accessTokenExpiresAt })
        },
        [storeTokens],
    )

    const loginWithProvider = useCallback(
        async (provider: string) => {
            const redirectUri = Linking.createURL('login')
            const authUrl = buildExternalLoginUrl(provider, redirectUri)
            const result = await WebBrowser.openAuthSessionAsync(authUrl, redirectUri)
            if (result.type !== 'success' || !result.url) {
                throw new AuthException('Sign-in was cancelled.')
            }
            // The API redirects back with the tokens in the URL fragment.
            const hash = result.url.split('#')[1] ?? ''
            const params = new URLSearchParams(hash)
            const accessToken = params.get('access_token')
            const refreshToken = params.get('refresh_token')
            const expiresAt = params.get('access_token_expires_at')
            if (!accessToken || !refreshToken || !expiresAt) {
                throw new AuthException('Sign-in did not return valid tokens.')
            }
            completeExternalLogin(accessToken, refreshToken, expiresAt)
        },
        [completeExternalLogin],
    )

    // Restore session on mount.
    useEffect(() => {
        let cancelled = false
            ; (async () => {
                await tokenStorage.load()
                await deviceInfo.load()
                const stored = tokenStorage.getAccessToken()
                if (stored) {
                    const expiry = tokenExpiryUtcMs(stored) ?? Date.now() + 5 * 60 * 1000
                    if (Date.now() < expiry) {
                        expiresAtRef.current = expiry
                        if (!cancelled) setIsAuthenticated(true)
                    } else {
                        const ok = await refresh()
                        if (!ok && !cancelled) clearAuth()
                    }
                }
                if (!cancelled) setRestoring(false)
            })()
        return () => {
            cancelled = true
        }
    }, [refresh, clearAuth])

    // Another device removed this one from the devices list.
    useEffect(() => {
        return messageStream.onSessionRevoked(() => {
            messageStream.stop().catch(() => { })
            clearAuth()
        })
    }, [clearAuth])

    // The session is no longer usable (revoked device, expired refresh token, …).
    useEffect(() => {
        return onUnauthorized(() => {
            messageStream.stop().catch(() => { })
            clearAuth()
        })
    }, [clearAuth])

    const value = useMemo<AuthContextValue>(
        () => ({
            isAuthenticated,
            restoring,
            externalProviders: EXTERNAL_PROVIDERS,
            login,
            register,
            logout,
            loginWithProvider,
            completeExternalLogin,
        }),
        [isAuthenticated, restoring, login, register, logout, loginWithProvider, completeExternalLogin],
    )

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext)
    if (!ctx) throw new Error('useAuth must be used within an AuthProvider.')
    return ctx
}
