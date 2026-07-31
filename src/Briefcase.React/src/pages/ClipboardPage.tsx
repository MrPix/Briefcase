import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { MessageKind, type Message } from '../types'
import { messagesApi } from '../services/messages'
import { e2eeService } from '../crypto/e2ee'
import { messageStream } from '../realtime/messageStream'
import { MessageCard } from '../components/MessageCard'
import { downloadFile } from '../utils/download'
import { getDateLabel, formatFileSize } from '../utils/format'
import { PaperclipIcon, PasteIcon, SendIcon, CloseIcon, PlusIcon, ClipboardIcon } from '../components/icons'

type Filter = 'all' | 'pinned' | 'file' | 'url' | 'text'
const MAX_PINNED_IN_CLIPBOARD = 3

function filterFromPath(pathname: string): Filter {
    switch (pathname.replace(/^\//, '').toLowerCase()) {
        case 'favorites':
            return 'pinned'
        case 'files':
            return 'file'
        case 'links':
            return 'url'
        case 'text':
            return 'text'
        default:
            return 'all'
    }
}

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

interface StagedFile {
    id: string
    file: File
}

export function ClipboardPage() {
    const location = useLocation()
    const navigate = useNavigate()
    const filter = filterFromPath(location.pathname)

    const [messages, setMessages] = useState<Message[] | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [newContent, setNewContent] = useState('')
    const [stagedFiles, setStagedFiles] = useState<StagedFile[]>([])
    const [isUploading, setIsUploading] = useState(false)
    const [isDragOver, setIsDragOver] = useState(false)
    const listEndRef = useRef<HTMLDivElement>(null)
    const listRef = useRef<HTMLDivElement>(null)
    const didInitialScrollRef = useRef(false)

    const loadMessages = useCallback(async () => {
        try {
            setError(null)
            await e2eeService.tryAutoUnlock()
            const items = await messagesApi.list()
            const decrypted = await Promise.all(items.map(decryptInPlace))
            setMessages(decrypted)
        } catch (err) {
            setMessages([])
            setError(`Failed to load messages: ${err instanceof Error ? err.message : String(err)}`)
        }
    }, [])

    useEffect(() => {
        loadMessages()
    }, [loadMessages])

    // Land at the newest message on first load/reload. Keep pinning to the bottom
    // for a short window so late reflow (lazy image previews, decrypt, real-time
    // upserts) can't drift the view, and bail out as soon as the user scrolls.
    useEffect(() => {
        if (didInitialScrollRef.current) return
        if (!messages || messages.length === 0) return
        didInitialScrollRef.current = true

        const el = listRef.current
        if (!el) return

        let cancelled = false
        const start = performance.now()
        const DURATION_MS = 800

        const stop = () => {
            cancelled = true
            el.removeEventListener('wheel', stop)
            el.removeEventListener('touchmove', stop)
            window.removeEventListener('keydown', stop)
        }
        el.addEventListener('wheel', stop, { passive: true })
        el.addEventListener('touchmove', stop, { passive: true })
        window.addEventListener('keydown', stop)

        const tick = () => {
            if (cancelled) return
            el.scrollTop = el.scrollHeight
            if (performance.now() - start < DURATION_MS) requestAnimationFrame(tick)
            else stop()
        }
        requestAnimationFrame(tick)

        return stop
    }, [messages])

    // Real-time updates from other devices.
    useEffect(() => {
        const upsert = (incoming: Message) => {
            decryptInPlace(incoming).then((m) => {
                setMessages((prev) => {
                    const list = prev ? [...prev] : []
                    const idx = list.findIndex((x) => x.id === m.id)
                    if (idx >= 0) list[idx] = m
                    else list.push(m)
                    return list
                })
            })
        }
        const remove = (id: string) => setMessages((prev) => prev?.filter((m) => m.id !== id) ?? prev)

        const unsub = [messageStream.onCreated(upsert), messageStream.onUpdated(upsert), messageStream.onRemoved(remove)]
        messageStream.start().catch(() => { })
        return () => unsub.forEach((u) => u())
    }, [])

    const { pinnedMessages, recentGroups } = useMemo(() => {
        const all = messages ?? []
        const filtered =
            filter === 'pinned'
                ? all.filter((m) => m.isPinned)
                : filter === 'file'
                    ? all.filter((m) => m.kind === MessageKind.File)
                    : filter === 'url'
                        ? all.filter((m) => m.kind === MessageKind.Url)
                        : filter === 'text'
                            ? all.filter((m) => m.kind === MessageKind.Text)
                            : all

        const pinned = filtered
            .filter((m) => m.isPinned)
            .sort((a, b) => new Date(b.pinnedAt ?? 0).getTime() - new Date(a.pinnedAt ?? 0).getTime())

        const unpinned = filter === 'all' || filter === 'pinned' ? filtered.filter((m) => !m.isPinned) : filtered

        const today = new Date()
        const groupMap = new Map<string, Message[]>()
        for (const m of unpinned) {
            const label = getDateLabel(new Date(m.createdAt), today)
            const arr = groupMap.get(label) ?? []
            arr.push(m)
            groupMap.set(label, arr)
        }
        const groups = [...groupMap.entries()]
            .map(([label, items]) => ({
                label,
                items: items.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()),
            }))
            .sort((a, b) => {
                const aMax = Math.max(...a.items.map((m) => new Date(m.createdAt).getTime()))
                const bMax = Math.max(...b.items.map((m) => new Date(m.createdAt).getTime()))
                return aMax - bMax
            })

        return { pinnedMessages: pinned, recentGroups: groups }
    }, [messages, filter])

    const visiblePinned = filter === 'all' ? pinnedMessages.slice(0, MAX_PINNED_IN_CLIPBOARD) : pinnedMessages
    const canSend = stagedFiles.length > 0 || newContent.trim().length > 0

    const sendMessage = async () => {
        if (!canSend) return
        try {
            if (stagedFiles.length > 0) {
                setIsUploading(true)
                let comment = newContent.trim() ? newContent : undefined
                for (const staged of stagedFiles) {
                    await messagesApi.uploadFile(staged.file, comment)
                    comment = undefined
                }
            } else {
                const isUrl = /^https?:\/\//i.test(newContent.trim())
                const kind = isUrl ? MessageKind.Url : MessageKind.Text
                let content = newContent
                let isEncrypted = false
                let encryptionIV: string | null = null

                await e2eeService.tryAutoUnlock()
                if (e2eeService.isUnlocked) {
                    const result = await e2eeService.encrypt(content)
                    content = result.ciphertext
                    encryptionIV = result.iv
                    isEncrypted = true
                }
                await messagesApi.create(kind, content, isEncrypted, encryptionIV)
            }
            setNewContent('')
            setStagedFiles([])
            await loadMessages()
            requestAnimationFrame(() => listEndRef.current?.scrollIntoView({ behavior: 'smooth' }))
        } catch (err) {
            setError(`Failed to send message: ${err instanceof Error ? err.message : String(err)}`)
        } finally {
            setIsUploading(false)
        }
    }

    const handlePaste = async () => {
        try {
            const text = await navigator.clipboard.readText()
            if (text) setNewContent(text)
        } catch {
            /* clipboard read may be blocked */
        }
    }

    const handlePin = async (m: Message) => {
        await messagesApi.togglePin(m.id)
        await loadMessages()
    }
    const handleDelete = async (m: Message) => {
        await messagesApi.remove(m.id)
        await loadMessages()
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
    const handleDownload = async (m: Message) => {
        if (!m.fileId) return
        try {
            await downloadFile(m.fileId)
        } catch (err) {
            setError(`Failed to download file: ${err instanceof Error ? err.message : String(err)}`)
        }
    }
    const handleEdit = async (m: Message, newText: string) => {
        try {
            let content: string | null = newText
            let isEncrypted = false
            let encryptionIV: string | null = null
            await e2eeService.tryAutoUnlock()
            if (e2eeService.isUnlocked && content) {
                const result = await e2eeService.encrypt(content)
                content = result.ciphertext
                encryptionIV = result.iv
                isEncrypted = true
            }
            await messagesApi.edit(m.id, content, isEncrypted, encryptionIV)
            await loadMessages()
        } catch (err) {
            setError(`Failed to edit message: ${err instanceof Error ? err.message : String(err)}`)
        }
    }
    const handleSendTo = (m: Message) => navigate(`/transfer?messageId=${m.id}`)

    const stageFiles = (files: FileList | null) => {
        if (!files) return
        const additions: StagedFile[] = []
        for (let i = 0; i < Math.min(files.length, 10); i++) {
            additions.push({ id: crypto.randomUUID(), file: files[i] })
        }
        setStagedFiles((prev) => [...prev, ...additions])
    }

    return (
        <div
            className={`clipboard-drop-zone${isDragOver ? ' drag-over' : ''}`}
            onDragEnter={(e) => {
                e.preventDefault()
                setIsDragOver(true)
            }}
            onDragOver={(e) => e.preventDefault()}
            onDragLeave={(e) => {
                e.preventDefault()
                setIsDragOver(false)
            }}
            onDrop={(e) => {
                e.preventDefault()
                setIsDragOver(false)
                stageFiles(e.dataTransfer.files)
            }}
        >
            {isDragOver && (
                <div className="drop-overlay">
                    <div className="drop-overlay-content">
                        <p>Drop files here to upload</p>
                    </div>
                </div>
            )}

            <div className="clipboard-layout">
                <div className="clipboard-list">
                    {isUploading && (
                        <div className="upload-progress">
                            <span>Uploading…</span>
                        </div>
                    )}

                    {error ? (
                        <div className="alert alert-danger mx-3">
                            {error}
                            <button className="btn btn-outline btn-sm" onClick={loadMessages} style={{ marginLeft: '0.5rem' }}>
                                Retry
                            </button>
                        </div>
                    ) : messages === null ? (
                        <div className="loading-state">
                            <p>Loading messages…</p>
                        </div>
                    ) : visiblePinned.length === 0 && recentGroups.length === 0 ? (
                        <div className="empty-state">
                            <ClipboardIcon size={48} style={{ stroke: 'var(--text-muted)', strokeWidth: 1 }} />
                            <p>No messages yet</p>
                            <span>Send your first message to get started</span>
                        </div>
                    ) : (
                        <div className="message-list" ref={listRef}>
                            {pinnedMessages.length > 0 && (filter === 'all' || filter === 'pinned') && (
                                <>
                                    <div className="list-section-header">Pinned</div>
                                    {visiblePinned.map((m) => (
                                        <div className="message-item" key={m.id}>
                                            <MessageCard
                                                message={m}
                                                onPin={handlePin}
                                                onDelete={handleDelete}
                                                onCopy={handleCopy}
                                                onDownload={handleDownload}
                                                onEdit={handleEdit}
                                                onSendTo={handleSendTo}
                                            />
                                        </div>
                                    ))}
                                    {filter === 'all' && pinnedMessages.length > MAX_PINNED_IN_CLIPBOARD && (
                                        <button className="see-all-link" onClick={() => navigate('/favorites')}>
                                            See all {pinnedMessages.length} pinned
                                        </button>
                                    )}
                                </>
                            )}

                            {filter !== 'pinned' &&
                                recentGroups.map((group) => (
                                    <div key={group.label}>
                                        <div className="list-section-header">{group.label}</div>
                                        {group.items.map((m) => (
                                            <div className="message-item" key={m.id}>
                                                <MessageCard
                                                    message={m}
                                                    onPin={handlePin}
                                                    onDelete={handleDelete}
                                                    onCopy={handleCopy}
                                                    onDownload={handleDownload}
                                                    onEdit={handleEdit}
                                                    onSendTo={handleSendTo}
                                                />
                                            </div>
                                        ))}
                                    </div>
                                ))}
                            <div ref={listEndRef} />
                        </div>
                    )}

                    <div className="compose-area">
                        {stagedFiles.length > 0 && (
                            <div className="staged-files">
                                {stagedFiles.map((staged) => (
                                    <div className="staged-file" key={staged.id}>
                                        <span className="staged-file-name">{staged.file.name}</span>
                                        <span className="staged-file-size">{formatFileSize(staged.file.size)}</span>
                                        <button
                                            className="staged-file-remove"
                                            title="Remove"
                                            onClick={() => setStagedFiles((prev) => prev.filter((f) => f.id !== staged.id))}
                                        >
                                            <CloseIcon size={12} />
                                        </button>
                                    </div>
                                ))}
                                <label className="staged-file-add" title="Add more files">
                                    <PlusIcon size={14} /> Add files
                                    <input type="file" multiple style={{ display: 'none' }} onChange={(e) => stageFiles(e.target.files)} />
                                </label>
                            </div>
                        )}

                        <textarea
                            className="compose-input"
                            rows={3}
                            value={newContent}
                            onChange={(e) => setNewContent(e.target.value)}
                            placeholder={stagedFiles.length > 0 ? 'Add a comment (optional)…' : 'Type or paste a message…'}
                        />

                        <div className="compose-actions">
                            {stagedFiles.length === 0 && (
                                <label className="toolbar-btn" title="Attach file">
                                    <PaperclipIcon size={14} />
                                    <input type="file" multiple style={{ display: 'none' }} onChange={(e) => stageFiles(e.target.files)} />
                                </label>
                            )}
                            <button className="toolbar-btn" title="Paste from clipboard" onClick={handlePaste}>
                                <PasteIcon size={14} />
                            </button>
                            <button className="toolbar-btn primary" onClick={sendMessage} disabled={!canSend}>
                                <SendIcon size={14} /> Send
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}
