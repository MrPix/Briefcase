import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MessageKind, type SharedMessage } from '../types'
import { shareApi } from '../services/share'
import { LinkIcon, FileIcon, DownloadIcon, BackIcon, WarningIcon } from '../components/icons'
import { formatTimestamp } from '../utils/format'

export function ShareViewPage() {
    const { slug = '' } = useParams()
    const navigate = useNavigate()
    const [loading, setLoading] = useState(true)
    const [message, setMessage] = useState<SharedMessage | null>(null)
    const [notFound, setNotFound] = useState(false)

    useEffect(() => {
        let cancelled = false
            ; (async () => {
                try {
                    const result = await shareApi.getSharedMessage(slug)
                    if (cancelled) return
                    setMessage(result)
                    setNotFound(result === null)
                } catch {
                    if (!cancelled) setNotFound(true)
                } finally {
                    if (!cancelled) setLoading(false)
                }
            })()
        return () => {
            cancelled = true
        }
    }, [slug])

    return (
        <div className="share-container">
            {loading ? (
                <div className="share-card">
                    <div className="share-loading">
                        <span className="spinner" />
                        <span>Loading shared message…</span>
                    </div>
                </div>
            ) : notFound || !message ? (
                <div className="share-card share-error-card">
                    <div className="share-error-icon">
                        <WarningIcon size={48} style={{ stroke: '#f87171' }} />
                    </div>
                    <h2 className="share-title">Link Unavailable</h2>
                    <p className="share-subtitle">This link has already been used, expired, or does not exist.</p>
                    <button className="share-back-btn" onClick={() => navigate('/')}>
                        <BackIcon size={14} /> Back
                    </button>
                </div>
            ) : (
                <div className="share-card">
                    <div className="share-badge">
                        <LinkIcon size={14} /> Shared message
                    </div>

                    <div className="share-content">
                        {message.kind === MessageKind.File ? (
                            <>
                                <div className="share-file">
                                    <FileIcon size={40} style={{ stroke: '#dc2626' }} />
                                    <span className="share-file-name">{message.fileName ?? message.content ?? 'Attached file'}</span>
                                </div>
                                {message.previewUrl && (
                                    <img className="share-preview" src={message.previewUrl} alt="Preview" loading="lazy" />
                                )}
                                {message.downloadUrl && (
                                    <a className="btn btn-primary share-download-btn" href={message.downloadUrl} download>
                                        <DownloadIcon size={16} /> Download
                                    </a>
                                )}
                            </>
                        ) : (
                            <div className="share-text">{message.content}</div>
                        )}
                    </div>

                    <div className="share-meta">Shared on {formatTimestamp(message.createdAt)}</div>

                    <button className="share-back-btn" onClick={() => navigate('/')}>
                        <BackIcon size={14} /> Back
                    </button>
                </div>
            )}
        </div>
    )
}
