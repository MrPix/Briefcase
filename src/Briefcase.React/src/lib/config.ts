/// <reference types="vite/client" />

// Base URL of the Briefcase API.
// - In production the SPA is served by Caddy which reverse-proxies /api and
//   /hubs on the same origin, so this is empty and all requests are relative.
// - In development Aspire injects VITE_API_BASE_URL pointing at the API service.
export const API_BASE_URL: string = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

/** Builds an absolute API URL from a relative path such as `api/messages`. */
export function apiUrl(path: string): string {
    const clean = path.startsWith('/') ? path : `/${path}`
    return `${API_BASE_URL}${clean}`
}
