import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { AuthException, useAuth } from '../auth/AuthContext'
import { devicesApi } from '../services/devices'
import { messagesApi } from '../services/messages'
import { e2eeService } from '../crypto/e2ee'
import { platformLabel, type Device, type E2eeSettings } from '../types'

export function SettingsPage() {
    const { changePassword, logout } = useAuth()
    const navigate = useNavigate()

    const [devices, setDevices] = useState<Device[] | null>(null)

    // Change password
    const [currentPassword, setCurrentPassword] = useState('')
    const [newPassword, setNewPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [passwordMessage, setPasswordMessage] = useState<string | null>(null)
    const [passwordSuccess, setPasswordSuccess] = useState(false)
    const [isChangingPassword, setIsChangingPassword] = useState(false)

    // E2EE
    const [e2eeSettings, setE2eeSettings] = useState<E2eeSettings | null>(null)
    const [unlocked, setUnlocked] = useState(e2eeService.isUnlocked)
    const [e2eeMessage, setE2eeMessage] = useState<string | null>(null)
    const [e2eeSuccess, setE2eeSuccess] = useState(false)
    const [isE2eeWorking, setIsE2eeWorking] = useState(false)
    const [rememberPassphrase, setRememberPassphrase] = useState(false)

    const [showEnableForm, setShowEnableForm] = useState(false)
    const [showUnlockForm, setShowUnlockForm] = useState(false)
    const [showChangePassphraseForm, setShowChangePassphraseForm] = useState(false)
    const [enablePass, setEnablePass] = useState('')
    const [enablePassConfirm, setEnablePassConfirm] = useState('')
    const [unlockPass, setUnlockPass] = useState('')
    const [newPass, setNewPass] = useState('')
    const [newPassConfirm, setNewPassConfirm] = useState('')

    const [isLoggingOut, setIsLoggingOut] = useState(false)

    useEffect(() => {
        ; (async () => {
            try {
                setDevices(await devicesApi.list())
            } catch {
                setDevices([])
            }
            setE2eeSettings((await e2eeService.getSettings()) ?? { isEnabled: false, kdfAlgorithm: null, kdfSalt: null, kdfParams: null, keyVerifier: null })
            setRememberPassphrase(e2eeService.getRememberPassphrase())
            setUnlocked(e2eeService.isUnlocked)
        })()
    }, [])

    const refreshSettings = async () => {
        setE2eeSettings(await e2eeService.getSettings())
        setUnlocked(e2eeService.isUnlocked)
    }

    const handleChangePassword = async (e: React.FormEvent) => {
        e.preventDefault()
        setPasswordMessage(null)
        setPasswordSuccess(false)
        if (newPassword !== confirmPassword) {
            setPasswordMessage('Passwords do not match.')
            return
        }
        if (newPassword.length < 8) {
            setPasswordMessage('Password must be at least 8 characters.')
            return
        }
        setIsChangingPassword(true)
        try {
            await changePassword(currentPassword, newPassword)
            setPasswordMessage('Password changed successfully.')
            setPasswordSuccess(true)
            setCurrentPassword('')
            setNewPassword('')
            setConfirmPassword('')
        } catch (err) {
            setPasswordMessage(err instanceof AuthException ? err.message : `An error occurred: ${String(err)}`)
        } finally {
            setIsChangingPassword(false)
        }
    }

    const handleEnableE2ee = async (e: React.FormEvent) => {
        e.preventDefault()
        setE2eeMessage(null)
        if (enablePass !== enablePassConfirm) {
            setE2eeMessage('Passphrases do not match.')
            setE2eeSuccess(false)
            return
        }
        if (enablePass.length < 8) {
            setE2eeMessage('Passphrase must be at least 8 characters.')
            setE2eeSuccess(false)
            return
        }
        setIsE2eeWorking(true)
        try {
            await e2eeService.enable(enablePass)
            await refreshSettings()
            setShowEnableForm(false)
            setEnablePass('')
            setEnablePassConfirm('')
            setE2eeMessage('E2EE enabled. Your messages will now be encrypted.')
            setE2eeSuccess(true)
        } catch (err) {
            setE2eeMessage(`Failed to enable E2EE: ${err instanceof Error ? err.message : String(err)}`)
            setE2eeSuccess(false)
        } finally {
            setIsE2eeWorking(false)
        }
    }

    const handleDisableE2ee = async () => {
        setIsE2eeWorking(true)
        setE2eeMessage(null)
        try {
            await e2eeService.disable()
            await refreshSettings()
            setE2eeMessage('E2EE disabled.')
            setE2eeSuccess(true)
        } catch (err) {
            setE2eeMessage(`Failed to disable E2EE: ${err instanceof Error ? err.message : String(err)}`)
            setE2eeSuccess(false)
        } finally {
            setIsE2eeWorking(false)
        }
    }

    const handleUnlockE2ee = async (e: React.FormEvent) => {
        e.preventDefault()
        setIsE2eeWorking(true)
        setE2eeMessage(null)
        try {
            const ok = await e2eeService.tryUnlock(unlockPass)
            if (ok) {
                setShowUnlockForm(false)
                setUnlockPass('')
                setUnlocked(true)
                setE2eeMessage('Vault unlocked. Messages will be decrypted.')
                setE2eeSuccess(true)
            } else {
                setE2eeMessage('Incorrect passphrase.')
                setE2eeSuccess(false)
            }
        } catch (err) {
            setE2eeMessage(`Unlock failed: ${err instanceof Error ? err.message : String(err)}`)
            setE2eeSuccess(false)
        } finally {
            setIsE2eeWorking(false)
        }
    }

    const handleChangePassphrase = async (e: React.FormEvent) => {
        e.preventDefault()
        setE2eeMessage(null)
        if (newPass !== newPassConfirm) {
            setE2eeMessage('Passphrases do not match.')
            setE2eeSuccess(false)
            return
        }
        if (newPass.length < 8) {
            setE2eeMessage('Passphrase must be at least 8 characters.')
            setE2eeSuccess(false)
            return
        }
        setIsE2eeWorking(true)
        try {
            // Re-key all existing encrypted messages with the new passphrase.
            const encrypted = (await messagesApi.list({ pageSize: 500 })).filter((m) => m.isEncrypted)
            await e2eeService.changePassphrase(newPass, encrypted, (id, ciphertext, iv) =>
                messagesApi.edit(id, ciphertext, true, iv),
            )
            setShowChangePassphraseForm(false)
            setNewPass('')
            setNewPassConfirm('')
            setE2eeMessage('Passphrase changed.')
            setE2eeSuccess(true)
        } catch (err) {
            setE2eeMessage(`Failed to change passphrase: ${err instanceof Error ? err.message : String(err)}`)
            setE2eeSuccess(false)
        } finally {
            setIsE2eeWorking(false)
        }
    }

    const onRememberChanged = (remember: boolean) => {
        setRememberPassphrase(remember)
        try {
            e2eeService.setRememberPassphrase(remember)
        } catch (err) {
            setE2eeMessage(`Failed to update remember-passphrase setting: ${err instanceof Error ? err.message : String(err)}`)
            setE2eeSuccess(false)
        }
    }

    const handleLogout = async () => {
        setIsLoggingOut(true)
        e2eeService.lock()
        await logout()
        navigate('/login', { replace: true })
    }

    return (
        <div className="settings-container">
            <div className="settings-card">
                <h2 className="settings-title">Settings</h2>

                {/* ── Devices ── */}
                <div className="settings-section">
                    <h5>Device</h5>
                    {devices === null ? (
                        <p className="text-muted">
                            <em>Loading devices…</em>
                        </p>
                    ) : devices.length === 0 ? (
                        <p className="text-muted">No registered devices.</p>
                    ) : (
                        <ul className="settings-device-list">
                            {devices.map((d) => (
                                <li key={d.id}>
                                    <div>
                                        <strong>{d.name}</strong>
                                        <span className="badge-type badge-text device-platform">{platformLabel(d.platform)}</span>
                                    </div>
                                    <small className="text-muted">Last seen: {new Date(d.lastSeenAt).toLocaleString()}</small>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* ── Change Password ── */}
                <div className="settings-section">
                    <h5>Change Password</h5>
                    {passwordMessage && (
                        <div className={`alert ${passwordSuccess ? 'alert-success' : 'alert-danger'}`}>{passwordMessage}</div>
                    )}
                    <form onSubmit={handleChangePassword}>
                        <div className="mb-3">
                            <label className="form-label">Current Password</label>
                            <input type="password" className="form-control" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} required />
                        </div>
                        <div className="mb-3">
                            <label className="form-label">New Password</label>
                            <input type="password" className="form-control" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required />
                        </div>
                        <div className="mb-3">
                            <label className="form-label">Confirm New Password</label>
                            <input type="password" className="form-control" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
                        </div>
                        <button type="submit" className="btn btn-primary" disabled={isChangingPassword}>
                            {isChangingPassword ? 'Changing…' : 'Change Password'}
                        </button>
                    </form>
                </div>

                {/* ── E2EE ── */}
                <div className="settings-section">
                    <h5>End-to-End Encryption</h5>
                    <p className="text-muted">
                        When enabled, messages are encrypted in your browser before being sent to the server. Your passphrase never
                        leaves your device.
                    </p>
                    {e2eeMessage && <div className={`alert ${e2eeSuccess ? 'alert-success' : 'alert-danger'}`}>{e2eeMessage}</div>}

                    {e2eeSettings === null ? (
                        <p className="text-muted">
                            <em>Loading…</em>
                        </p>
                    ) : !e2eeSettings.isEnabled ? (
                        showEnableForm ? (
                            <form onSubmit={handleEnableE2ee}>
                                <div className="mb-3">
                                    <label className="form-label">Passphrase</label>
                                    <input type="password" className="form-control" value={enablePass} onChange={(e) => setEnablePass(e.target.value)} required />
                                </div>
                                <div className="mb-3">
                                    <label className="form-label">Confirm Passphrase</label>
                                    <input type="password" className="form-control" value={enablePassConfirm} onChange={(e) => setEnablePassConfirm(e.target.value)} required />
                                </div>
                                <div className="settings-btn-row">
                                    <button type="submit" className="btn btn-primary" disabled={isE2eeWorking}>
                                        Enable
                                    </button>
                                    <button type="button" className="btn btn-outline" onClick={() => setShowEnableForm(false)}>
                                        Cancel
                                    </button>
                                </div>
                            </form>
                        ) : (
                            <button className="btn btn-outline" onClick={() => setShowEnableForm(true)}>
                                Enable E2EE
                            </button>
                        )
                    ) : (
                        <>
                            <div className="settings-e2ee-status">
                                <span className="badge-type badge-file">Enabled</span>
                                {unlocked ? (
                                    <span className="badge-type badge-text">Unlocked</span>
                                ) : (
                                    <span className="badge-type badge-link">Locked</span>
                                )}
                            </div>

                            <label className="settings-remember">
                                <input type="checkbox" checked={rememberPassphrase} onChange={(e) => onRememberChanged(e.target.checked)} />
                                <span>Remember passphrase on this browser</span>
                            </label>
                            <div className="text-muted settings-remember-hint">When off, passphrase is kept only for this tab/session.</div>

                            {!unlocked &&
                                (showUnlockForm ? (
                                    <form onSubmit={handleUnlockE2ee}>
                                        <div className="mb-3">
                                            <label className="form-label">Passphrase</label>
                                            <input type="password" className="form-control" value={unlockPass} onChange={(e) => setUnlockPass(e.target.value)} required />
                                        </div>
                                        <div className="settings-btn-row">
                                            <button type="submit" className="btn btn-primary" disabled={isE2eeWorking}>
                                                Unlock
                                            </button>
                                            <button type="button" className="btn btn-outline" onClick={() => setShowUnlockForm(false)}>
                                                Cancel
                                            </button>
                                        </div>
                                    </form>
                                ) : (
                                    <button className="btn btn-outline" onClick={() => setShowUnlockForm(true)}>
                                        Unlock
                                    </button>
                                ))}

                            {showChangePassphraseForm ? (
                                <form onSubmit={handleChangePassphrase}>
                                    <div className="mb-3">
                                        <label className="form-label">New Passphrase</label>
                                        <input type="password" className="form-control" value={newPass} onChange={(e) => setNewPass(e.target.value)} required />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">Confirm New Passphrase</label>
                                        <input type="password" className="form-control" value={newPassConfirm} onChange={(e) => setNewPassConfirm(e.target.value)} required />
                                    </div>
                                    <div className="settings-btn-row">
                                        <button type="submit" className="btn btn-primary" disabled={isE2eeWorking}>
                                            Change Passphrase
                                        </button>
                                        <button type="button" className="btn btn-outline" onClick={() => setShowChangePassphraseForm(false)}>
                                            Cancel
                                        </button>
                                    </div>
                                </form>
                            ) : (
                                <div className="settings-btn-row">
                                    <button className="btn btn-outline btn-sm" disabled={!unlocked} onClick={() => setShowChangePassphraseForm(true)}>
                                        Change Passphrase
                                    </button>
                                    <button className="btn btn-danger btn-sm" onClick={handleDisableE2ee} disabled={isE2eeWorking}>
                                        Disable E2EE
                                    </button>
                                </div>
                            )}
                        </>
                    )}
                </div>

                {/* ── Logout ── */}
                <div className="settings-section">
                    <button className="btn btn-danger btn-block" onClick={handleLogout} disabled={isLoggingOut}>
                        {isLoggingOut ? 'Logging out…' : 'Log Out'}
                    </button>
                </div>
            </div>
        </div>
    )
}
