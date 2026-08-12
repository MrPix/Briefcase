import { useCallback, useEffect, useRef, useState } from 'react'
import type { Message } from '../types'
import { trashApi } from '../services/trash'
import { e2eeService } from '../crypto/e2ee'
import { MessageCard } from '../components/MessageCard'
import { TrashIcon } from '../components/icons'

async function decryptInPlace(message: Message): Promise<Message> {
    if (e2eeService.isUnlocked && message.isEncrypted && message.content && message.encryptionIV) {
        try {
            return { ...message, content: await e2eeService.decrypt(message.content, message.encryptionIV) }
        } catch {
            /* leave ciphertext */
        }
    }
    return message
}

export function TrashPage() {
    const [messages, setMessages] = useState<Message[] | null>(null)
    const [error, setError] = useState<string | null>(null)
    const didLoadMessages = useRef(false)

    const loadMessages = useCallback(async () => {
        try {
            setError(null)
            await e2eeService.tryAutoUnlock()
            const items = await trashApi.list()
            const decrypted = await Promise.all(
                items
                    .slice()
                    .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
                    .map(decryptInPlace),
            )
            setMessages(decrypted)
        } catch (err) {
            setMessages([])
            setError(`Failed to load trashed messages: ${err instanceof Error ? err.message : String(err)}`)
        }
    }, [])

    useEffect(() => {
        if (didLoadMessages.current) return
        didLoadMessages.current = true
        loadMessages()
    }, [loadMessages])

    const handleRestore = async (m: Message) => {
        try {
            await trashApi.restore(m.id)
            await loadMessages()
        } catch (err) {
            setError(`Failed to restore message: ${err instanceof Error ? err.message : String(err)}`)
        }
    }

    const handleCopy = async (m: Message) => {
        if (m.content) {
            try {
                await navigator.clipboard.writeText(m.content)
            } catch {
                /* ignore */
            }
        }
    }

    return (
        <div className="clipboard-layout">
            <div className="clipboard-list">
                {error ? (
                    <div className="alert alert-danger mx-3">
                        {error}
                        <button className="btn btn-outline btn-sm" onClick={loadMessages} style={{ marginLeft: '0.5rem' }}>
                            Retry
                        </button>
                    </div>
                ) : messages === null ? (
                    <div className="loading-state">
                        <p>Loading trashed messages…</p>
                    </div>
                ) : messages.length === 0 ? (
                    <div className="empty-state">
                        <TrashIcon size={48} style={{ stroke: 'var(--text-muted)', strokeWidth: 1 }} />
                        <p>Trash is empty</p>
                        <span>Deleted messages will appear here</span>
                    </div>
                ) : (
                    <div className="message-list">
                        <div className="list-section-header">Deleted Messages</div>
                        {messages.map((m) => (
                            <div className="message-item" key={m.id}>
                                <MessageCard message={m} onRestore={handleRestore} onCopy={handleCopy} />
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    )
}
