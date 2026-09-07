import type { HubConnection } from '@microsoft/signalr'
import { buildMessageHubConnection } from './hub'
import type { Message } from '../types'

type MessageHandler = (message: Message) => void
type RemovedHandler = (id: string) => void
type VoidHandler = () => void

/**
 * Single authenticated SignalR connection to /hubs/messages that fans out
 * server-pushed message changes to subscribers.
 */
class MessageStreamService {
    private connection: HubConnection | null = null
    private starting: Promise<void> | null = null
    private readonly created = new Set<MessageHandler>()
    private readonly updated = new Set<MessageHandler>()
    private readonly removed = new Set<RemovedHandler>()
    private readonly sessionRevoked = new Set<VoidHandler>()

    onCreated(handler: MessageHandler): () => void {
        this.created.add(handler)
        return () => this.created.delete(handler)
    }
    onUpdated(handler: MessageHandler): () => void {
        this.updated.add(handler)
        return () => this.updated.delete(handler)
    }
    onRemoved(handler: RemovedHandler): () => void {
        this.removed.add(handler)
        return () => this.removed.delete(handler)
    }
    onSessionRevoked(handler: VoidHandler): () => void {
        this.sessionRevoked.add(handler)
        return () => this.sessionRevoked.delete(handler)
    }

    async start(): Promise<void> {
        if (this.connection) return
        if (this.starting) return this.starting

        const connection = buildMessageHubConnection()
        connection.on('MessageCreated', (m: Message) => this.created.forEach((h) => h(m)))
        connection.on('MessageUpdated', (m: Message) => this.updated.forEach((h) => h(m)))
        connection.on('MessageRestored', (m: Message) => this.created.forEach((h) => h(m)))
        connection.on('MessageTrashed', (p: { id: string }) => this.removed.forEach((h) => h(p.id)))
        connection.on('MessageDeleted', (p: { id: string }) => this.removed.forEach((h) => h(p.id)))
        connection.on('SessionRevoked', () => this.sessionRevoked.forEach((h) => h()))

        this.starting = connection
            .start()
            .then(() => {
                this.connection = connection
            })
            .finally(() => {
                this.starting = null
            })

        return this.starting
    }

    async stop(): Promise<void> {
        const c = this.connection
        this.connection = null
        if (c) await c.stop()
    }
}

export const messageStream = new MessageStreamService()
