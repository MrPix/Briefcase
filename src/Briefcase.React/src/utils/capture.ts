export type CaptureKind = 'text' | 'url'

export interface CaptureInfo {
    kind: CaptureKind
    value: string
    hostname?: string
    label?: string
}

export function detectCapture(value: string): CaptureInfo {
    const trimmed = value.trim()

    try {
        const url = new URL(trimmed)
        if (url.protocol === 'http:' || url.protocol === 'https:') {
            const hostname = url.hostname.replace(/^www\./i, '')
            const isGoogleMaps = hostname === 'maps.app.goo.gl' || (hostname.startsWith('maps.') && hostname.endsWith('.google.com'))
            return { kind: 'url', value: trimmed, hostname, label: isGoogleMaps ? 'Google Maps' : hostname }
        }
    } catch {
        // Treat incomplete or plain text input as a note.
    }

    return { kind: 'text', value }
}

export function compactUrl(value: string, maxLength = 72): string {
    const withoutProtocol = value.replace(/^https?:\/\//i, '').replace(/\/$/, '')
    return withoutProtocol.length > maxLength ? `${withoutProtocol.slice(0, maxLength - 1)}…` : withoutProtocol
}