import { useEffect, useRef, useState } from 'react'
import { BrowserQRCodeReader, type IScannerControls } from '@zxing/browser'
import { CloseIcon } from './icons'

export interface QrScannerProps {
    onScanned: (value: string) => void
}

export function QrScanner({ onScanned }: QrScannerProps) {
    const [scanning, setScanning] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const videoRef = useRef<HTMLVideoElement>(null)
    const controlsRef = useRef<IScannerControls | null>(null)

    const supported = typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getUserMedia

    useEffect(() => {
        if (!scanning || !videoRef.current) return
        let cancelled = false
        const reader = new BrowserQRCodeReader()

        reader
            .decodeFromVideoDevice(undefined, videoRef.current, (result, _err, controls) => {
                controlsRef.current = controls
                if (cancelled) return
                if (result) {
                    controls.stop()
                    setScanning(false)
                    onScanned(result.getText())
                }
            })
            .catch((e) => {
                if (!cancelled) {
                    setError(e instanceof Error ? e.message : 'Unable to start the camera.')
                    setScanning(false)
                }
            })

        return () => {
            cancelled = true
            controlsRef.current?.stop()
            controlsRef.current = null
        }
    }, [scanning, onScanned])

    if (!supported) {
        return <div className="alert alert-info">QR scanning is not supported on this device.</div>
    }

    return (
        <div className="qr-scanner">
            {scanning ? (
                <div className="qr-scanner-active">
                    <video ref={videoRef} className="qr-video" muted playsInline />
                    <p className="text-muted">Point your camera at a QR code</p>
                    <button className="btn btn-outline" onClick={() => setScanning(false)}>
                        <CloseIcon size={16} /> Cancel
                    </button>
                </div>
            ) : (
                <button className="btn btn-primary" onClick={() => setScanning(true)}>
                    Scan QR Code
                </button>
            )}
            {error && <div className="alert alert-danger mt-2">{error}</div>}
        </div>
    )
}
