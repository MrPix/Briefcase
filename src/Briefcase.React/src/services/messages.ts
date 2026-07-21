import { api } from '../lib/apiClient'
import { MessageKind, type Message, type PagedResponse, type ShareLinkResult } from '../types'

export interface MessageQuery {
    page?: number
    pageSize?: number
    kind?: MessageKind
    pinned?: boolean
    q?: string
}

function buildQuery(query: MessageQuery): string {
    const params = new URLSearchParams()
    params.set('page', String(query.page ?? 1))
    params.set('pageSize', String(query.pageSize ?? 50))
    if (query.kind !== undefined) params.set('kind', String(query.kind))
    if (query.pinned !== undefined) params.set('pinned', String(query.pinned))
    if (query.q) params.set('q', query.q)
    return params.toString()
}

interface FileUploadResponse {
    id: string
    originalName: string
    contentType: string
    sizeBytes: number
    createdAt: string
}

export const messagesApi = {
    async list(query: MessageQuery = {}): Promise<Message[]> {
        const paged = await api.get<PagedResponse<Message>>(`api/messages?${buildQuery(query)}`)
        return paged.items
    },

    create(kind: MessageKind, content: string, isEncrypted = false, encryptionIV: string | null = null): Promise<Message> {
        return api.post<Message>('api/messages', { kind, content, isEncrypted, encryptionIV })
    },

    edit(id: string, content: string | null, isEncrypted = false, encryptionIV: string | null = null): Promise<void> {
        return api.put<void>(`api/messages/${id}`, { content, isEncrypted, encryptionIV })
    },

    remove(id: string): Promise<void> {
        return api.del<void>(`api/messages/${id}`)
    },

    togglePin(id: string): Promise<Message> {
        return api.patch<Message>(`api/messages/${id}/pin`)
    },

    async uploadFile(file: File, comment?: string): Promise<Message> {
        const form = new FormData()
        form.append('file', file, file.name)
        const res = await api.raw('api/files', { method: 'POST', rawBody: form })
        if (!res.ok) throw new Error(`File upload failed (${res.status})`)
        const uploaded = (await res.json()) as FileUploadResponse
        const content = comment?.trim() ? comment : file.name
        return api.post<Message>('api/messages', { kind: MessageKind.File, content, fileId: uploaded.id })
    },

    async createShareLink(id: string, oneTime: boolean, expiresInMinutes: number | null): Promise<ShareLinkResult> {
        return api.post<ShareLinkResult>(`api/messages/${id}/share`, { oneTime, expiresInMinutes })
    },

    revokeShareLink(id: string): Promise<void> {
        return api.del<void>(`api/messages/${id}/share`)
    },
}
