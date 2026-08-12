import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { MessageKind, type SharedMessage } from '../types'
import { shareApi } from '../services/share'
import { LinkIcon, FileIcon, DownloadIcon, BackIcon, WarningIcon } from '../components/icons'
import { formatTimestamp } from '../utils/format'

export function ShareViewPage() {
    const { t } = useTranslation()
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
                        <span>{t('share.loading')}</span>
                    </div>
                </div>
            ) : notFound || !message ? (
                <div className="share-card share-error-card">
                    <div className="share-error-icon">
                        <WarningIcon size={48} style={{ stroke: '#f87171' }} />
                    </div>
                    <h2 className="share-title">{t('share.unavailableTitle')}</h2>
                    <p className="share-subtitle">{t('share.unavailableSubtitle')}</p>
                    <button className="share-back-btn" onClick={() => navigate('/')}>
                        <BackIcon size={14} /> {t('common.back')}
                    </button>
                </div>
            ) : (
                <div className="share-card">
                    <div className="share-badge">
                        <LinkIcon size={14} /> {t('share.badge')}
                    </div>

                    <div className="share-content">
                        {message.kind === MessageKind.File ? (
                            <>
                                <div className="share-file">
                                    <FileIcon size={40} style={{ stroke: '#dc2626' }} />
                                    <span className="share-file-name">{message.fileName ?? message.content ?? t('share.attachedFile')}</span>
                                </div>
                                {message.previewUrl && (
                                    <img className="share-preview" src={message.previewUrl} alt="Preview" loading="lazy" />
                                )}
                                {message.downloadUrl && (
                                    <a className="btn btn-primary share-download-btn" href={message.downloadUrl} download>
                                        <DownloadIcon size={16} /> {t('share.download')}
                                    </a>
                                )}
                            </>
                        ) : (
                            <div className="share-text">{message.content}</div>
                        )}
                    </div>

                    <div className="share-meta">{t('share.sharedOn', { date: formatTimestamp(message.createdAt) })}</div>

                    <button className="share-back-btn" onClick={() => navigate('/')}>
                        <BackIcon size={14} /> {t('common.back')}
                    </button>
                </div>
            )}
        </div>
    )
}
