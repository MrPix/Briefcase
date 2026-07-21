import { useEffect, useState } from 'react'
import QRCode from 'qrcode'
import { Platform, platformLabel, type Device } from '../types'
import { devicesApi } from '../services/devices'
import { QrScanner } from '../components/QrScanner'
import { TrashIcon } from '../components/icons'

export function DevicesPage() {
    const [devices, setDevices] = useState<Device[] | null>(null)
    const [pairQr, setPairQr] = useState<string | null>(null)
    const [pairToken, setPairToken] = useState<string | null>(null)
    const [showScanner, setShowScanner] = useState(false)

    const [showAddDevice, setShowAddDevice] = useState(false)
    const [addDeviceCode, setAddDeviceCode] = useState('')
    const [isApproving, setIsApproving] = useState(false)
    const [addDeviceError, setAddDeviceError] = useState<string | null>(null)
    const [addDeviceSuccess, setAddDeviceSuccess] = useState<string | null>(null)

    const loadDevices = async () => {
        try {
            setDevices(await devicesApi.list())
        } catch {
            setDevices([])
        }
    }

    useEffect(() => {
        loadDevices()
    }, [])

    const generatePairCode = async () => {
        const res = await devicesApi.generatePairCode()
        setPairToken(res.token)
        setPairQr(await QRCode.toDataURL(res.token, { width: 240, margin: 1 }))
    }

    const handlePairCodeScanned = async (token: string) => {
        try {
            await devicesApi.claim(token, 'Paired device', Platform.Web)
        } finally {
            setShowScanner(false)
            await loadDevices()
        }
    }

    const removeDevice = async (id: string) => {
        await devicesApi.remove(id)
        await loadDevices()
    }

    const approveLoginCode = async () => {
        if (isApproving || !addDeviceCode.trim()) return
        setIsApproving(true)
        setAddDeviceError(null)
        setAddDeviceSuccess(null)
        try {
            const result = await devicesApi.approveLoginCode(addDeviceCode.trim())
            setAddDeviceSuccess(`"${result.deviceName}" has been signed in.`)
            setAddDeviceCode('')
            await loadDevices()
        } catch (err) {
            setAddDeviceError(err instanceof Error ? err.message : String(err))
        } finally {
            setIsApproving(false)
        }
    }

    return (
        <div className="devices-page">
            <h1>Devices</h1>

            <div className="devices-toolbar">
                <button className="btn btn-primary" onClick={generatePairCode}>
                    Generate Pair Code
                </button>
                <button className="btn btn-outline" onClick={() => setShowScanner((v) => !v)}>
                    Scan Pair Code
                </button>
                <button
                    className="btn btn-outline"
                    onClick={() => {
                        setShowAddDevice((v) => !v)
                        setAddDeviceError(null)
                        setAddDeviceSuccess(null)
                    }}
                >
                    Add device
                </button>
            </div>

            {showAddDevice && (
                <div className="card devices-card">
                    <h5>Add a device with a code</h5>
                    <p className="text-muted">
                        On the new device, choose <strong>Add this device</strong> to get an 8-character code, then enter it below.
                    </p>
                    <div className="devices-code-row">
                        <input
                            className="form-control"
                            maxLength={8}
                            placeholder="Enter code"
                            value={addDeviceCode}
                            onChange={(e) => setAddDeviceCode(e.target.value.toUpperCase())}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') approveLoginCode()
                            }}
                            disabled={isApproving}
                            style={{ textTransform: 'uppercase', maxWidth: '16rem' }}
                        />
                        <button className="btn btn-primary" onClick={approveLoginCode} disabled={isApproving || !addDeviceCode.trim()}>
                            {isApproving ? <span className="spinner" /> : 'Add'}
                        </button>
                    </div>
                    {addDeviceError && <div className="alert alert-danger mt-2">{addDeviceError}</div>}
                    {addDeviceSuccess && <div className="alert alert-success mt-2">{addDeviceSuccess}</div>}
                </div>
            )}

            {pairQr && (
                <div className="alert alert-info devices-pair">
                    <p>Scan this code with the new device to pair it:</p>
                    <img src={pairQr} alt="Pairing QR code" className="devices-pair-qr" />
                    <details>
                        <summary>Show pairing token</summary>
                        <code className="devices-pair-token">{pairToken}</code>
                    </details>
                </div>
            )}

            {showScanner && (
                <div className="devices-scanner">
                    <QrScanner onScanned={handlePairCodeScanned} />
                </div>
            )}

            {devices === null ? (
                <p>
                    <em>Loading devices…</em>
                </p>
            ) : devices.length === 0 ? (
                <p className="text-muted">No registered devices.</p>
            ) : (
                <div className="device-list">
                    {devices.map((device) => (
                        <div className="device-item" key={device.id}>
                            <div>
                                <strong>{device.name}</strong>
                                <span className="badge-type badge-text device-platform">{platformLabel(device.platform)}</span>
                                <br />
                                <small className="text-muted">Last seen: {new Date(device.lastSeenAt).toLocaleString()}</small>
                            </div>
                            <button className="btn btn-outline btn-sm" onClick={() => removeDevice(device.id)}>
                                <TrashIcon size={16} /> Remove
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    )
}
