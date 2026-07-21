import { api } from '../lib/apiClient'
import { apiUrl } from '../lib/config'
import { MessageKind, type SharedMessage } from '../types'

interface SharedMessageDto {
    messageId: string
    kind: string
    content: string | null
    fileId: string | null
    fileName: string | null
    filePreviewToken: string | null
    fileDownloadToken: string | null
    createdAt: string
}

function parseKind(kind: string): MessageKind {
    switch (kind.toLowerCase()) {
        case 'url':
            return MessageKind.Url
        case 'file':
            return MessageKind.File
        default:
            return MessageKind.Text
    }
}

export const shareApi = {
    /** Public (unauthenticated) fetch of a shared message by slug. */
    async getSharedMessage(slug: string): Promise<SharedMessage | null> {
        const res = await api.raw(`api/share/${encodeURIComponent(slug)}`, { method: 'GET', skipAuth: true })
        if (res.status === 404) return null
        if (!res.ok) throw new Error(`Failed to load shared message (${res.status})`)
        const dto = (await res.json()) as SharedMessageDto

        const previewUrl = dto.filePreviewToken
            ? apiUrl(`/api/share/${encodeURIComponent(slug)}/preview?token=${encodeURIComponent(dto.filePreviewToken)}`)
            : null
        const downloadUrl = dto.fileDownloadToken
            ? apiUrl(`/api/share/${encodeURIComponent(slug)}/file?token=${encodeURIComponent(dto.fileDownloadToken)}`)
            : null

        return {
            messageId: dto.messageId,
            kind: parseKind(dto.kind),
            content: dto.content,
            fileId: dto.fileId,
            fileName: dto.fileName,
            previewUrl,
            downloadUrl,
            createdAt: dto.createdAt,
        }
    },
}
