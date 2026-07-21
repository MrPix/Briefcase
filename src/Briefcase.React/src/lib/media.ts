import { apiUrl } from './config'
import { tokenStorage } from '../auth/tokenStorage'

/**
 * Resolves a message's relative preview path (e.g. `/api/files/{id}/preview`)
 * to an absolute URL with the access token appended, because <img> elements
 * cannot send an Authorization header. The API allows a query-string token for
 * the /api/files path (see JwtBearer OnMessageReceived).
 */
export function previewImageUrl(filePreviewUrl: string | null): string | null {
    if (!filePreviewUrl) return null
    const token = tokenStorage.getAccessToken()
    const sep = filePreviewUrl.includes('?') ? '&' : '?'
    const withToken = token ? `${filePreviewUrl}${sep}access_token=${encodeURIComponent(token)}` : filePreviewUrl
    return apiUrl(withToken)
}

/** Absolute authenticated download URL for a file attachment. */
export function fileDownloadUrl(fileId: string): string {
    const token = tokenStorage.getAccessToken()
    const q = token ? `?access_token=${encodeURIComponent(token)}` : ''
    return apiUrl(`/api/files/${fileId}${q}`)
}
