import { api } from '../lib/apiClient'
import { MessageKind, type Message, type PagedResponse } from '../types'

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

/** A file chosen from the camera, gallery, or document picker. */
export interface PickedFile {
    uri: string
    name: string
    mimeType: string
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

    create(
        kind: MessageKind,
        content: string,
        isEncrypted = false,
        encryptionIV: string | null = null,
    ): Promise<Message> {
        return api.post<Message>('api/messages', { kind, content, isEncrypted, encryptionIV })
    },

    edit(id: string, content: string | null): Promise<void> {
        return api.put<void>(`api/messages/${id}`, { content, isEncrypted: false, encryptionIV: null })
    },

    remove(id: string): Promise<void> {
        return api.del<void>(`api/messages/${id}`)
    },

    togglePin(id: string): Promise<Message> {
        return api.patch<Message>(`api/messages/${id}/pin`)
    },

    async uploadFile(file: PickedFile, comment?: string): Promise<Message> {
        const form = new FormData()
        // React Native FormData accepts a { uri, name, type } file descriptor.
        form.append('file', {
            uri: file.uri,
            name: file.name,
            type: file.mimeType,
        } as unknown as Blob)

        const res = await api.raw('api/files', { method: 'POST', rawBody: form })
        if (!res.ok) throw new Error(`File upload failed (${res.status})`)
        const uploaded = (await res.json()) as FileUploadResponse
        const content = comment?.trim() ? comment : file.name
        return api.post<Message>('api/messages', {
            kind: MessageKind.File,
            content,
            fileId: uploaded.id,
        })
    },
}
