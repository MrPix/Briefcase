import Constants from 'expo-constants'

// Base URL of the Briefcase API, injected from app.config.ts (extra.apiBaseUrl).
// Unlike the web client this must be an ABSOLUTE URL — React Native has no origin.
const configured =
    (Constants.expoConfig?.extra?.apiBaseUrl as string | undefined) ??
    process.env.EXPO_PUBLIC_API_BASE_URL ??
    ''

export const API_BASE_URL: string = configured.replace(/\/$/, '')

/** Builds an absolute API URL from a relative path such as `api/messages`. */
export function apiUrl(path: string): string {
    const clean = path.startsWith('/') ? path : `/${path}`
    return `${API_BASE_URL}${clean}`
}
