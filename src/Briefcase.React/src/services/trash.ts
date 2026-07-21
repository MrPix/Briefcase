import { api } from '../lib/apiClient'
import type { Message, PagedResponse } from '../types'

export const trashApi = {
    async list(page = 1, pageSize = 50): Promise<Message[]> {
        const paged = await api.get<PagedResponse<Message>>(`api/trash?page=${page}&pageSize=${pageSize}`)
        return paged.items
    },
    restore(id: string): Promise<Message> {
        return api.post<Message>(`api/trash/${id}/restore`)
    },
}
