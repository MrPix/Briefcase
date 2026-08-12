import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { MessageKind, type Message } from '../types'
import { previewImageUrl } from '../lib/media'
import { linkify } from '../utils/linkify'
import { formatTimestamp } from '../utils/format'
import {
    CopyIcon,
    DownloadIcon,
    EditIcon,
    FileIcon,
    LinkIcon,
    PinIcon,
    RestoreIcon,
    SendIcon,
    TextIcon,
    TrashIcon,
} from './icons'

export interface MessageCardProps {
    message: Message
    onPin?: (m: Message) => void
    onDelete?: (m: Message) => void
    onCopy?: (m: Message) => void
    onDownload?: (m: Message) => void
    onEdit?: (m: Message, newContent: string) => void
    onSendTo?: (m: Message) => void
    onRestore?: (m: Message) => void
}

export function MessageCard({
    message,
    onPin,
    onDelete,
    onCopy,
    onDownload,
    onEdit,
    onSendTo,
    onRestore,
}: MessageCardProps) {
    const { t } = useTranslation()
    const [isEditing, setIsEditing] = useState(false)
    const [editContent, setEditContent] = useState('')

    const hasExplicitFileName = !!message.fileName?.trim()
    const fileNameToShow = hasExplicitFileName
        ? message.fileName!
        : message.content?.trim()
            ? message.content!
            : t('messageCard.attachedFile')
    const fileComment =
        hasExplicitFileName && message.content?.trim() && message.content !== message.fileName
            ? message.content
            : null
    const previewSrc = message.filePreviewUrl ? previewImageUrl(message.filePreviewUrl) : null

    const startEdit = () => {
        setEditContent(message.content ?? '')
        setIsEditing(true)
    }
    const saveEdit = () => {
        onEdit?.(message, editContent)
        setIsEditing(false)
    }

    return (
        <div className={`msg-card${message.isPinned ? ' pinned' : ''}`}>
            <div className="msg-icon">
                {message.kind === MessageKind.Url ? (
                    <LinkIcon size={24} style={{ stroke: 'var(--accent)' }} />
                ) : message.kind === MessageKind.File ? (
                    <FileIcon size={24} style={{ stroke: '#dc2626' }} />
                ) : (
                    <TextIcon size={24} style={{ stroke: 'var(--text-secondary)' }} />
                )}
            </div>

            <div className="msg-body">
                <div className="msg-top-row">
                    <span className="msg-time">{formatTimestamp(message.createdAt)}</span>
                </div>

                {isEditing ? (
                    <div className="msg-edit">
                        <textarea
                            className="msg-edit-input"
                            rows={2}
                            value={editContent}
                            onChange={(e) => setEditContent(e.target.value)}
                        />
                        <div className="msg-edit-actions">
                            <button className="msg-edit-btn save" onClick={saveEdit}>
                                {t('messageCard.save')}
                            </button>
                            <button className="msg-edit-btn cancel" onClick={() => setIsEditing(false)}>
                                {t('messageCard.cancel')}
                            </button>
                        </div>
                    </div>
                ) : message.kind === MessageKind.File ? (
                    <>
                        <div className="msg-file-name">{fileNameToShow}</div>
                        {previewSrc && (
                            <img className="msg-image-preview" src={previewSrc} alt={fileNameToShow} loading="lazy" />
                        )}
                        {fileComment && <div className="msg-file-comment">{linkify(fileComment)}</div>}
                    </>
                ) : (
                    <div className="msg-preview">{linkify(message.content)}</div>
                )}
            </div>

            <div className="msg-actions">
                {message.kind === MessageKind.File && message.fileId && onDownload && (
                    <button className="msg-action-btn" title={t('messageCard.download')} onClick={() => onDownload(message)}>
                        <DownloadIcon size={16} />
                    </button>
                )}
                {onCopy && (
                    <button className="msg-action-btn" title={t('messageCard.copy')} onClick={() => onCopy(message)}>
                        <CopyIcon size={16} />
                    </button>
                )}
                {onPin && (
                    <button
                        className="msg-action-btn"
                        title={message.isPinned ? t('messageCard.unpin') : t('messageCard.pin')}
                        onClick={() => onPin(message)}
                    >
                        <PinIcon size={16} style={message.isPinned ? { fill: 'var(--accent)' } : undefined} />
                    </button>
                )}
                {onEdit && (
                    <button className="msg-action-btn" title={t('messageCard.edit')} onClick={startEdit}>
                        <EditIcon size={16} />
                    </button>
                )}
                {onSendTo && (
                    <button className="msg-action-btn" title={t('messageCard.share')} onClick={() => onSendTo(message)}>
                        <SendIcon size={16} />
                    </button>
                )}
                {onRestore && (
                    <button className="msg-action-btn" title={t('messageCard.restore')} onClick={() => onRestore(message)}>
                        <RestoreIcon size={16} />
                    </button>
                )}
                {onDelete && (
                    <button className="msg-action-btn delete" title={t('messageCard.delete')} onClick={() => onDelete(message)}>
                        <TrashIcon size={16} />
                    </button>
                )}
            </div>
        </div>
    )
}
