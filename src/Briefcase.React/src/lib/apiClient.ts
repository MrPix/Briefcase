import { apiUrl } from './config'
import { tokenStorage } from '../auth/tokenStorage'
import type { AuthResponse } from '../types'

export class ApiError extends Error {
    status: number
    title?: string
    constructor(status: number, message: string, title?: string) {
        super(message)
        this.status = status
        this.title = title
    }
}

// A single in-flight refresh shared across concurrent 401s.
let refreshPromise: Promise<boolean> | null = null

const unauthorizedHandlers = new Set<() => void>()

/** Notified when a request stays 401 after a refresh attempt, i.e. the session is gone. */
export function onUnauthorized(handler: () => void): () => void {
    unauthorizedHandlers.add(handler)
    return () => unauthorizedHandlers.delete(handler)
}

async function tryRefresh(): Promise<boolean> {
    const refreshToken = tokenStorage.getRefreshToken()
    if (!refreshToken) return false

    refreshPromise ??= (async () => {
        try {
            const res = await fetch(apiUrl('api/auth/refresh'), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken }),
            })
            if (!res.ok) return false
            const data = (await res.json()) as AuthResponse
            tokenStorage.setAccessToken(data.accessToken)
            if (data.refreshToken) tokenStorage.setRefreshToken(data.refreshToken)
            return true
        } catch {
            return false
        } finally {
            refreshPromise = null
        }
    })()

    return refreshPromise
}

export interface RequestOptions extends Omit<RequestInit, 'body'> {
    body?: unknown
    /** When true, do not attempt token refresh/retry on 401. */
    skipAuth?: boolean
    /** When set, send FormData / Blob as-is instead of JSON. */
    rawBody?: BodyInit
}

async function extractErrorTitle(res: Response): Promise<string | undefined> {
    try {
        const data = await res.clone().json()
        return data?.title ?? data?.error
    } catch {
        return undefined
    }
}

async function doFetch(path: string, options: RequestOptions): Promise<Response> {
    const headers = new Headers(options.headers)
    const token = tokenStorage.getAccessToken()
    if (token) headers.set('Authorization', `Bearer ${token}`)

    let body: BodyInit | undefined
    if (options.rawBody !== undefined) {
        body = options.rawBody
    } else if (options.body !== undefined) {
        headers.set('Content-Type', 'application/json')
        body = JSON.stringify(options.body)
    }

    return fetch(apiUrl(path), { ...options, headers, body })
}

export async function apiFetch(path: string, options: RequestOptions = {}): Promise<Response> {
    let res = await doFetch(path, options)

    const isAuthEndpoint = path.includes('/api/auth/') || path.startsWith('api/auth/')
    if (res.status === 401 && !options.skipAuth && !isAuthEndpoint) {
        const refreshed = await tryRefresh()
        if (refreshed) {
            res = await doFetch(path, options)
        }

        if (res.status === 401) {
            tokenStorage.clear()
            unauthorizedHandlers.forEach((handler) => handler())
        }
    }

    return res
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
    const res = await apiFetch(path, options)
    if (!res.ok) {
        const title = await extractErrorTitle(res)
        throw new ApiError(res.status, title ?? `Request failed (${res.status})`, title)
    }
    if (res.status === 204) return undefined as T
    const text = await res.text()
    return (text ? JSON.parse(text) : undefined) as T
}

type ExtraOptions = Pick<RequestOptions, 'skipAuth' | 'headers'>

export const api = {
    get: <T>(path: string, options?: ExtraOptions) => request<T>(path, { ...options, method: 'GET' }),
    post: <T>(path: string, body?: unknown, options?: ExtraOptions) =>
        request<T>(path, { ...options, method: 'POST', body }),
    put: <T>(path: string, body?: unknown, options?: ExtraOptions) =>
        request<T>(path, { ...options, method: 'PUT', body }),
    patch: <T>(path: string, body?: unknown, options?: ExtraOptions) =>
        request<T>(path, { ...options, method: 'PATCH', body }),
    del: <T>(path: string, options?: ExtraOptions) => request<T>(path, { ...options, method: 'DELETE' }),
    raw: apiFetch,
}
