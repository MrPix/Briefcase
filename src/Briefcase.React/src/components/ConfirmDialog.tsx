import { useEffect, useId, useRef } from 'react'

interface ConfirmDialogProps {
    open: boolean
    title: string
    description: string
    confirmLabel: string
    cancelLabel: string
    isConfirming?: boolean
    onConfirm: () => Promise<void>
    onCancel: () => void
}

export function ConfirmDialog({
    open,
    title,
    description,
    confirmLabel,
    cancelLabel,
    isConfirming = false,
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    const dialogRef = useRef<HTMLDivElement>(null)
    const cancelButtonRef = useRef<HTMLButtonElement>(null)
    const titleId = useId()
    const descriptionId = useId()

    useEffect(() => {
        if (!open) return

        cancelButtonRef.current?.focus()
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape' && !isConfirming) {
                onCancel()
                return
            }
            if (event.key !== 'Tab') return

            const focusable = dialogRef.current?.querySelectorAll<HTMLElement>(
                'button:not(:disabled), [href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"])',
            )
            if (!focusable || focusable.length === 0) return

            const first = focusable[0]
            const last = focusable[focusable.length - 1]
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault()
                last.focus()
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault()
                first.focus()
            }
        }

        document.addEventListener('keydown', handleKeyDown)
        return () => document.removeEventListener('keydown', handleKeyDown)
    }, [isConfirming, onCancel, open])

    if (!open) return null

    return (
        <div
            className="confirm-dialog-backdrop"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget && !isConfirming) onCancel()
            }}
        >
            <div
                ref={dialogRef}
                className="confirm-dialog"
                role="dialog"
                aria-modal="true"
                aria-labelledby={titleId}
                aria-describedby={descriptionId}
            >
                <h2 id={titleId}>{title}</h2>
                <p id={descriptionId}>{description}</p>
                <div className="confirm-dialog-actions">
                    <button ref={cancelButtonRef} className="btn btn-outline" onClick={onCancel} disabled={isConfirming}>
                        {cancelLabel}
                    </button>
                    <button className="btn btn-danger" onClick={() => void onConfirm()} disabled={isConfirming}>
                        {isConfirming ? <span className="spinner" /> : confirmLabel}
                    </button>
                </div>
            </div>
        </div>
    )
}