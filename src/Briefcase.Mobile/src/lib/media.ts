import { File, Paths } from 'expo-file-system'
import * as Sharing from 'expo-sharing'
import { apiUrl } from './config'
import { tokenStorage } from '../auth/tokenStorage'

/**
 * Resolves a message's relative preview path (e.g. `/api/files/{id}/preview`)
 * to an absolute URL with the access token appended, because <Image> requests
 * are simplest to authenticate via the query token the API already accepts.
 */
export function previewImageUrl(filePreviewUrl: string | null): string | null {
    if (!filePreviewUrl) return null
    const token = tokenStorage.getAccessToken()
    const sep = filePreviewUrl.includes('?') ? '&' : '?'
    const withToken = token
        ? `${filePreviewUrl}${sep}access_token=${encodeURIComponent(token)}`
        : filePreviewUrl
    return apiUrl(withToken)
}

/** Absolute authenticated download URL for a file attachment. */
export function fileDownloadUrl(fileId: string): string {
    const token = tokenStorage.getAccessToken()
    const q = token ? `?access_token=${encodeURIComponent(token)}` : ''
    return apiUrl(`/api/files/${fileId}${q}`)
}

/**
 * Downloads an authenticated attachment to a cache file and opens the native
 * share sheet so the user can save or forward it.
 */
export async function downloadAndShare(fileId: string, fileName: string): Promise<void> {
    const token = tokenStorage.getAccessToken()
    const destination = new File(Paths.cache, fileName || `download-${fileId}`)
    if (destination.exists) destination.delete()

    const downloaded = await File.downloadFileAsync(apiUrl(`/api/files/${fileId}`), destination, {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    })

    if (await Sharing.isAvailableAsync()) {
        await Sharing.shareAsync(downloaded.uri)
    }
}
