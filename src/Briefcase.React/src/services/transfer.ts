import { HubConnectionBuilder } from '@microsoft/signalr'
import { api } from '../lib/apiClient'
import { apiUrl } from '../lib/config'
import type { PairCodeResponse } from '../types'

interface SessionResponse {
    code: string
}

let pendingSessionRequest: Promise<string> | null = null

export const transferApi = {
    async createSession(): Promise<string> {
        if (pendingSessionRequest) return pendingSessionRequest

        pendingSessionRequest = api
            .post<SessionResponse>('api/transfer/session', undefined, { skipAuth: true })
            .then((res) => res.code)
            .finally(() => {
                pendingSessionRequest = null
            })

        return pendingSessionRequest
    },

    pushContent(sessionId: string, content: string): Promise<void> {
        return api.post<void>('api/transfer/push', { sessionId, content }, { skipAuth: true })
    },

    sendTo(code: string, messageId: string): Promise<void> {
        return api.post<void>('api/transfer/send', { code, messageId })
    },

    /**
     * Joins a transfer session over SignalR and invokes onUrlReceived when the
     * paired device pushes a URL. Resolves when the signal aborts.
     */
    async listenForTransfer(
        code: string,
        onUrlReceived: (url: string) => void,
        signal: AbortSignal,
    ): Promise<void> {
        const connection = new HubConnectionBuilder().withUrl(apiUrl('/hubs/messages')).build()
        connection.on('TransferReceived', (data: { url?: string }) => {
            if (data?.url) onUrlReceived(data.url)
        })

        try {
            await connection.start()
            await connection.invoke('JoinTransferSession', code)
            await new Promise<void>((resolve) => {
                if (signal.aborted) return resolve()
                signal.addEventListener('abort', () => resolve())
            })
        } finally {
            await connection.stop()
        }
    },
}

export type { PairCodeResponse }
