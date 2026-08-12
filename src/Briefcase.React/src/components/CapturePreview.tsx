import { FileIcon, LinkIcon, CloseIcon } from './icons'
import { compactUrl, detectCapture } from '../utils/capture'
import { formatFileSize } from '../utils/format'

interface StagedFile {
    id: string
    file: File
}

interface CapturePreviewProps {
    content: string
    files: StagedFile[]
    onRemoveFile: (id: string) => void
}

export function CapturePreview({ content, files, onRemoveFile }: CapturePreviewProps) {
    const capture = detectCapture(content)

    return (
        <div className="capture-preview" aria-live="polite">
            {files.map(({ id, file }) => (
                <div className="capture-file-card" key={id}>
                    <FileIcon size={20} />
                    <div className="capture-preview-copy">
                        <strong>{file.name}</strong>
                        <span>{file.type || 'File'} · {formatFileSize(file.size)}</span>
                    </div>
                    <button className="capture-remove" title={`Remove ${file.name}`} onClick={() => onRemoveFile(id)}>
                        <CloseIcon size={15} />
                    </button>
                </div>
            ))}

            {files.length === 0 && capture.kind === 'url' && (
                <div className="capture-url-card">
                    <LinkIcon size={22} />
                    <div className="capture-preview-copy">
                        <strong>{capture.label}</strong>
                        <span>{compactUrl(capture.value)}</span>
                    </div>
                </div>
            )}
        </div>
    )
}