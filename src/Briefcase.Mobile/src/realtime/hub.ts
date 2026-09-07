import { HubConnectionBuilder, HttpTransportType, type HubConnection } from '@microsoft/signalr'
import { apiUrl } from '../lib/config'
import { tokenStorage } from '../auth/tokenStorage'

/**
 * Builds a SignalR hub connection to /hubs/messages with JWT auth + reconnect.
 * React Native has no EventSource, so ServerSentEvents is omitted — only
 * WebSockets and LongPolling are negotiated.
 */
export function buildMessageHubConnection(): HubConnection {
    return new HubConnectionBuilder()
        .withUrl(apiUrl('/hubs/messages'), {
            accessTokenFactory: () => tokenStorage.getAccessToken() ?? '',
            transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect()
        .build()
}
