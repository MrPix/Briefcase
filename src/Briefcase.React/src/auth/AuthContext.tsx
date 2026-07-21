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
import { apiFetch } from '../lib/apiClient'
import { API_BASE_URL } from '../lib/config'
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
    changePassword: (currentPassword: string, newPassword: string) => Promise<void>
    completeExternalLogin: (accessToken: string, refreshToken: string, accessTokenExpiresAt: string) => void
    buildExternalLoginUrl: (provider: string, clientRedirectUri: string) => string
}

const AuthContext = createContext<AuthContextValue | null>(null)

function tokenExpiryUtcMs(token: string): number | null {
    try {
        const payload = token.split('.')[1]
        if (!payload) return null
        const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
        const json = atob(normalized)
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

export function AuthProvider({ children }: { children: ReactNode }) {
    const [isAuthenticated, setIsAuthenticated] = useState(false)
    const [restoring, setRestoring] = useState(true)
    const expiresAtRef = useRef<number>(0)

    const storeTokens = useCallback((result: AuthResponse) => {
        tokenStorage.setAccessToken(result.accessToken)
        tokenStorage.setRefreshToken(result.refreshToken)
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
        const result = (await res.json()) as AuthResponse
        storeTokens(result)
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
        clearAuth()
    }, [clearAuth])

    const changePassword = useCallback(async (currentPassword: string, newPassword: string) => {
        const res = await apiFetch('api/auth/change-password', {
            method: 'POST',
            body: { currentPassword, newPassword },
        })
        if (!res.ok) throw new AuthException(await readProblemTitle(res, 'Failed to change password.'))
    }, [])

    const completeExternalLogin = useCallback(
        (accessToken: string, refreshToken: string, accessTokenExpiresAt: string) => {
            storeTokens({ accessToken, refreshToken, accessTokenExpiresAt })
        },
        [storeTokens],
    )

    const buildExternalLoginUrl = useCallback((provider: string, clientRedirectUri: string): string => {
        if (!EXTERNAL_PROVIDERS.some((p) => p.key.toLowerCase() === provider.toLowerCase())) {
            throw new AuthException(`Unsupported external auth provider: ${provider}.`)
        }
        const base = API_BASE_URL || window.location.origin
        const encodedProvider = encodeURIComponent(provider)
        const callbackUri = `${base}/api/auth/oauth/${encodedProvider}/callback`
        const query =
            `redirect_uri=${encodeURIComponent(callbackUri)}` +
            `&client_redirect_uri=${encodeURIComponent(clientRedirectUri)}` +
            `&device_name=${encodeURIComponent(deviceInfo.deviceName)}` +
            `&device_platform=${encodeURIComponent(deviceInfo.platform)}`
        return `${base}/api/auth/oauth/${encodedProvider}?${query}`
    }, [])

    // Restore session on mount.
    useEffect(() => {
        let cancelled = false
            ; (async () => {
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

    const value = useMemo<AuthContextValue>(
        () => ({
            isAuthenticated,
            restoring,
            externalProviders: EXTERNAL_PROVIDERS,
            login,
            register,
            logout,
            changePassword,
            completeExternalLogin,
            buildExternalLoginUrl,
        }),
        [
            isAuthenticated,
            restoring,
            login,
            register,
            logout,
            changePassword,
            completeExternalLogin,
            buildExternalLoginUrl,
        ],
    )

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext)
    if (!ctx) throw new Error('useAuth must be used within an AuthProvider.')
    return ctx
}
