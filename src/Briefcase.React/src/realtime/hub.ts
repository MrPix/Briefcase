import { HubConnectionBuilder, HttpTransportType, type HubConnection } from '@microsoft/signalr'
import { apiUrl } from '../lib/config'
import { tokenStorage } from '../auth/tokenStorage'

/** Builds a SignalR hub connection to /hubs/messages with JWT auth + reconnect. */
export function buildMessageHubConnection(): HubConnection {
    return new HubConnectionBuilder()
        .withUrl(apiUrl('/hubs/messages'), {
            accessTokenFactory: () => tokenStorage.getAccessToken() ?? '',
            // Allow negotiation to fall back where WebSockets are blocked.
            transport:
                HttpTransportType.WebSockets |
                HttpTransportType.ServerSentEvents |
                HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect()
        .build()
}
