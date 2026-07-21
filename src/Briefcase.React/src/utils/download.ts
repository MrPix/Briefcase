import { fileDownloadUrl } from '../lib/media'
import { apiFetch } from '../lib/apiClient'

/** Downloads an authenticated file attachment by fetching it and saving the blob. */
export async function downloadFile(fileId: string): Promise<void> {
    const res = await apiFetch(`api/files/${fileId}`, { method: 'GET' })
    if (!res.ok) throw new Error(`Download failed (${res.status})`)

    const blob = await res.blob()
    const disposition = res.headers.get('Content-Disposition') ?? ''
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)
    const fileName = match ? decodeURIComponent(match[1]) : 'download'

    saveBlob(blob, fileName)
}

export function saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
}

export { fileDownloadUrl }
