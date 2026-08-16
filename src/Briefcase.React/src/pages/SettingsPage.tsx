import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AuthException, useAuth } from '../auth/AuthContext'
import { devicesApi } from '../services/devices'
import { messagesApi } from '../services/messages'
import { e2eeService } from '../crypto/e2ee'
import { platformLabel, type Device, type E2eeSettings } from '../types'
import { LanguageSwitcher } from '../components/LanguageSwitcher'
import { settingsApi, type UserSettings } from '../services/settings'

const APP_VERSION = import.meta.env.VITE_APP_VERSION ?? '1.0.0'

export function SettingsPage() {
    const { t } = useTranslation()
    const { changePassword, logout } = useAuth()
    const navigate = useNavigate()

    const [devices, setDevices] = useState<Device[] | null>(null)
    const [isSigningOutOthers, setIsSigningOutOthers] = useState(false)
    const [devicesMessage, setDevicesMessage] = useState<string | null>(null)
    const [devicesSuccess, setDevicesSuccess] = useState(false)

    // Language
    const [languageMessage, setLanguageMessage] = useState<string | null>(null)

    // Google Maps navigation
    const [navigationSettings, setNavigationSettings] = useState<UserSettings | null>(null)
    const [navigationEnabled, setNavigationEnabled] = useState(true)
    const [navigationApplicationIds, setNavigationApplicationIds] = useState<string[]>([])
    const [navigationMessage, setNavigationMessage] = useState<string | null>(null)
    const [navigationSuccess, setNavigationSuccess] = useState(false)
    const [isSavingNavigation, setIsSavingNavigation] = useState(false)

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
    const didLoadSettings = useRef(false)

    useEffect(() => {
        if (didLoadSettings.current) return
        didLoadSettings.current = true
            ; (async () => {
                try {
                    setDevices(await devicesApi.list())
                } catch {
                    setDevices([])
                }
                try {
                    const settings = await settingsApi.get()
                    setNavigationSettings(settings)
                    setNavigationEnabled(settings.googleMapsNavigationEnabled)
                    setNavigationApplicationIds(settings.navigationApplicationIds)
                } catch {
                    setNavigationMessage(t('settings.navigationLoadFailed'))
                }
                setE2eeSettings((await e2eeService.getSettings()) ?? { isEnabled: false, kdfAlgorithm: null, kdfSalt: null, kdfParams: null, keyVerifier: null })
                setRememberPassphrase(e2eeService.getRememberPassphrase())
                setUnlocked(e2eeService.isUnlocked)
            })()
    }, [t])

    const refreshSettings = async () => {
        setE2eeSettings(await e2eeService.getSettings())
        setUnlocked(e2eeService.isUnlocked)
    }

    const handleSignOutOthers = async () => {
        setIsSigningOutOthers(true)
        setDevicesMessage(null)
        try {
            const result = await devicesApi.signOutOthers()
            setDevices(await devicesApi.list())
            setDevicesMessage(t('settings.signOutOthersDone', { count: result.removedCount }))
            setDevicesSuccess(true)
        } catch (err) {
            setDevicesMessage(t('settings.signOutOthersFailed', { error: err instanceof Error ? err.message : String(err) }))
            setDevicesSuccess(false)
        } finally {
            setIsSigningOutOthers(false)
        }
    }

    const handleChangePassword = async (e: React.FormEvent) => {
        e.preventDefault()
        setPasswordMessage(null)
        setPasswordSuccess(false)
        if (newPassword !== confirmPassword) {
            setPasswordMessage(t('settings.passwordMismatch'))
            return
        }
        if (newPassword.length < 8) {
            setPasswordMessage(t('settings.passwordTooShort'))
            return
        }
        setIsChangingPassword(true)
        try {
            await changePassword(currentPassword, newPassword)
            setPasswordMessage(t('settings.passwordChanged'))
            setPasswordSuccess(true)
            setCurrentPassword('')
            setNewPassword('')
            setConfirmPassword('')
        } catch (err) {
            setPasswordMessage(err instanceof AuthException ? err.message : t('settings.passwordChangeGenericError', { error: String(err) }))
        } finally {
            setIsChangingPassword(false)
        }
    }

    const toggleNavigationApplication = (id: string) => {
        setNavigationApplicationIds((current) =>
            current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
        )
    }

    const handleSaveNavigation = async () => {
        setIsSavingNavigation(true)
        setNavigationMessage(null)
        try {
            const settings = await settingsApi.updateNavigation(navigationEnabled, navigationApplicationIds)
            setNavigationSettings(settings)
            setNavigationEnabled(settings.googleMapsNavigationEnabled)
            setNavigationApplicationIds(settings.navigationApplicationIds)
            setNavigationMessage(t('settings.navigationSaved'))
            setNavigationSuccess(true)
        } catch (err) {
            setNavigationMessage(t('settings.navigationSaveFailed', { error: err instanceof Error ? err.message : String(err) }))
            setNavigationSuccess(false)
        } finally {
            setIsSavingNavigation(false)
        }
    }

    const handleEnableE2ee = async (e: React.FormEvent) => {
        e.preventDefault()
        setE2eeMessage(null)
        if (enablePass !== enablePassConfirm) {
            setE2eeMessage(t('settings.passphraseMismatch'))
            setE2eeSuccess(false)
            return
        }
        if (enablePass.length < 8) {
            setE2eeMessage(t('settings.passphraseTooShort'))
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
            setE2eeMessage(t('settings.e2eeEnabled'))
            setE2eeSuccess(true)
        } catch (err) {
            setE2eeMessage(t('settings.e2eeEnableFailed', { error: err instanceof Error ? err.message : String(err) }))
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
            setE2eeMessage(t('settings.e2eeDisabled'))
            setE2eeSuccess(true)
        } catch (err) {
            setE2eeMessage(t('settings.e2eeDisableFailed', { error: err instanceof Error ? err.message : String(err) }))
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
                setE2eeMessage(t('settings.vaultUnlocked'))
                setE2eeSuccess(true)
            } else {
                setE2eeMessage(t('settings.incorrectPassphrase'))
                setE2eeSuccess(false)
            }
        } catch (err) {
            setE2eeMessage(t('settings.unlockFailed', { error: err instanceof Error ? err.message : String(err) }))
            setE2eeSuccess(false)
        } finally {
            setIsE2eeWorking(false)
        }
    }

    const handleChangePassphrase = async (e: React.FormEvent) => {
        e.preventDefault()
        setE2eeMessage(null)
        if (newPass !== newPassConfirm) {
            setE2eeMessage(t('settings.passphraseMismatch'))
            setE2eeSuccess(false)
            return
        }
        if (newPass.length < 8) {
            setE2eeMessage(t('settings.passphraseTooShort'))
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
            setE2eeMessage(t('settings.passphraseChanged'))
            setE2eeSuccess(true)
        } catch (err) {
            setE2eeMessage(t('settings.passphraseChangeFailed', { error: err instanceof Error ? err.message : String(err) }))
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
            setE2eeMessage(t('settings.rememberChangeFailed', { error: err instanceof Error ? err.message : String(err) }))
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
                <h2 className="settings-title">{t('settings.title')}</h2>

                {/* ── Devices ── */}
                <div className="settings-section">
                    <h5>{t('settings.deviceSection')}</h5>
                    {devices === null ? (
                        <p className="text-muted">
                            <em>{t('settings.loadingDevices')}</em>
                        </p>
                    ) : devices.length === 0 ? (
                        <p className="text-muted">{t('settings.noDevices')}</p>
                    ) : (
                        <ul className="settings-device-list">
                            {devices.map((d) => (
                                <li key={d.id}>
                                    <div>
                                        <strong>{d.name}</strong>
                                        <span className="badge-type badge-text device-platform">{platformLabel(d.platform)}</span>
                                        {d.isCurrent && <span className="badge-type badge-text device-platform">{t('settings.thisDevice')}</span>}
                                    </div>
                                    <small className="text-muted">{t('settings.lastSeen', { date: new Date(d.lastSeenAt).toLocaleString() })}</small>
                                </li>
                            ))}
                        </ul>
                    )}
                    {devices !== null && devices.length > 1 && (
                        <>
                            <p className="text-muted">{t('settings.signOutOthersHint')}</p>
                            <button className="btn btn-outline" onClick={handleSignOutOthers} disabled={isSigningOutOthers}>
                                {isSigningOutOthers ? <span className="spinner" /> : t('settings.signOutOthers')}
                            </button>
                        </>
                    )}
                    {devicesMessage && <div className={`alert ${devicesSuccess ? 'alert-success' : 'alert-danger'} mt-2`}>{devicesMessage}</div>}
                </div>

                {/* ── Language ── */}
                <div className="settings-section">
                    <h5>{t('settings.languageSection')}</h5>
                    <p className="text-muted">{t('settings.languageHint')}</p>
                    {languageMessage && <div className="alert alert-danger">{languageMessage}</div>}
                    <LanguageSwitcher
                        onChange={() => setLanguageMessage(null)}
                        onError={(err) => setLanguageMessage(t('settings.languageUpdateFailed', { error: err instanceof Error ? err.message : String(err) }))}
                    />
                </div>

                {/* ── Google Maps navigation ── */}
                <div className="settings-section">
                    <h5>{t('settings.navigationSection')}</h5>
                    <p className="text-muted">{t('settings.navigationHint')}</p>
                    {navigationMessage && (
                        <div className={`alert ${navigationSuccess ? 'alert-success' : 'alert-danger'}`}>{navigationMessage}</div>
                    )}
                    {navigationSettings === null ? (
                        <p className="text-muted"><em>{t('settings.loading')}</em></p>
                    ) : (
                        <>
                            <label className="settings-navigation-toggle">
                                <input
                                    type="checkbox"
                                    checked={navigationEnabled}
                                    onChange={(event) => setNavigationEnabled(event.target.checked)}
                                />
                                <span>{t('settings.navigationEnabled')}</span>
                            </label>
                            <div className="settings-navigation-apps">
                                {navigationSettings.navigationApplications.map((application) => (
                                    <label key={application.id} className="settings-navigation-app">
                                        <input
                                            type="checkbox"
                                            checked={navigationApplicationIds.includes(application.id)}
                                            disabled={!navigationEnabled}
                                            onChange={() => toggleNavigationApplication(application.id)}
                                        />
                                        <span>{application.displayName}</span>
                                    </label>
                                ))}
                            </div>
                            <button className="btn btn-primary" onClick={handleSaveNavigation} disabled={isSavingNavigation}>
                                {isSavingNavigation ? t('settings.navigationSaving') : t('settings.navigationSave')}
                            </button>
                        </>
                    )}
                </div>

                {/* ── Change Password ── */}
                <div className="settings-section">
                    <h5>{t('settings.changePasswordSection')}</h5>
                    {passwordMessage && (
                        <div className={`alert ${passwordSuccess ? 'alert-success' : 'alert-danger'}`}>{passwordMessage}</div>
                    )}
                    <form onSubmit={handleChangePassword}>
                        <div className="mb-3">
                            <label className="form-label">{t('settings.currentPassword')}</label>
                            <input type="password" className="form-control" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} required />
                        </div>
                        <div className="mb-3">
                            <label className="form-label">{t('settings.newPassword')}</label>
                            <input type="password" className="form-control" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required />
                        </div>
                        <div className="mb-3">
                            <label className="form-label">{t('settings.confirmNewPassword')}</label>
                            <input type="password" className="form-control" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
                        </div>
                        <button type="submit" className="btn btn-primary" disabled={isChangingPassword}>
                            {isChangingPassword ? t('settings.changingPassword') : t('settings.changePassword')}
                        </button>
                    </form>
                </div>

                {/* ── E2EE ── */}
                <div className="settings-section">
                    <h5>{t('settings.e2eeSection')}</h5>
                    <p className="text-muted">{t('settings.e2eeDescription')}</p>
                    {e2eeMessage && <div className={`alert ${e2eeSuccess ? 'alert-success' : 'alert-danger'}`}>{e2eeMessage}</div>}

                    {e2eeSettings === null ? (
                        <p className="text-muted">
                            <em>{t('settings.loading')}</em>
                        </p>
                    ) : !e2eeSettings.isEnabled ? (
                        showEnableForm ? (
                            <form onSubmit={handleEnableE2ee}>
                                <div className="mb-3">
                                    <label className="form-label">{t('settings.passphrase')}</label>
                                    <input type="password" className="form-control" value={enablePass} onChange={(e) => setEnablePass(e.target.value)} required />
                                </div>
                                <div className="mb-3">
                                    <label className="form-label">{t('settings.confirmPassphrase')}</label>
                                    <input type="password" className="form-control" value={enablePassConfirm} onChange={(e) => setEnablePassConfirm(e.target.value)} required />
                                </div>
                                <div className="settings-btn-row">
                                    <button type="submit" className="btn btn-primary" disabled={isE2eeWorking}>
                                        {t('settings.enable')}
                                    </button>
                                    <button type="button" className="btn btn-outline" onClick={() => setShowEnableForm(false)}>
                                        {t('common.cancel')}
                                    </button>
                                </div>
                            </form>
                        ) : (
                            <button className="btn btn-outline" onClick={() => setShowEnableForm(true)}>
                                {t('settings.enableE2ee')}
                            </button>
                        )
                    ) : (
                        <>
                            <div className="settings-e2ee-status">
                                <span className="badge-type badge-file">{t('settings.enabled')}</span>
                                {unlocked ? (
                                    <span className="badge-type badge-text">{t('settings.unlocked')}</span>
                                ) : (
                                    <span className="badge-type badge-link">{t('settings.locked')}</span>
                                )}
                            </div>

                            <label className="settings-remember">
                                <input type="checkbox" checked={rememberPassphrase} onChange={(e) => onRememberChanged(e.target.checked)} />
                                <span>{t('settings.rememberPassphrase')}</span>
                            </label>
                            <div className="text-muted settings-remember-hint">{t('settings.rememberPassphraseHint')}</div>

                            {!unlocked &&
                                (showUnlockForm ? (
                                    <form onSubmit={handleUnlockE2ee}>
                                        <div className="mb-3">
                                            <label className="form-label">{t('settings.passphrase')}</label>
                                            <input type="password" className="form-control" value={unlockPass} onChange={(e) => setUnlockPass(e.target.value)} required />
                                        </div>
                                        <div className="settings-btn-row">
                                            <button type="submit" className="btn btn-primary" disabled={isE2eeWorking}>
                                                {t('settings.unlock')}
                                            </button>
                                            <button type="button" className="btn btn-outline" onClick={() => setShowUnlockForm(false)}>
                                                {t('common.cancel')}
                                            </button>
                                        </div>
                                    </form>
                                ) : (
                                    <button className="btn btn-outline" onClick={() => setShowUnlockForm(true)}>
                                        {t('settings.unlock')}
                                    </button>
                                ))}

                            {showChangePassphraseForm ? (
                                <form onSubmit={handleChangePassphrase}>
                                    <div className="mb-3">
                                        <label className="form-label">{t('settings.newPassphrase')}</label>
                                        <input type="password" className="form-control" value={newPass} onChange={(e) => setNewPass(e.target.value)} required />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">{t('settings.confirmNewPassphrase')}</label>
                                        <input type="password" className="form-control" value={newPassConfirm} onChange={(e) => setNewPassConfirm(e.target.value)} required />
                                    </div>
                                    <div className="settings-btn-row">
                                        <button type="submit" className="btn btn-primary" disabled={isE2eeWorking}>
                                            {t('settings.changePassphrase')}
                                        </button>
                                        <button type="button" className="btn btn-outline" onClick={() => setShowChangePassphraseForm(false)}>
                                            {t('common.cancel')}
                                        </button>
                                    </div>
                                </form>
                            ) : (
                                <div className="settings-btn-row">
                                    <button className="btn btn-outline btn-sm" disabled={!unlocked} onClick={() => setShowChangePassphraseForm(true)}>
                                        {t('settings.changePassphrase')}
                                    </button>
                                    <button className="btn btn-danger btn-sm" onClick={handleDisableE2ee} disabled={isE2eeWorking}>
                                        {t('settings.disableE2ee')}
                                    </button>
                                </div>
                            )}
                        </>
                    )}
                </div>

                {/* ── Logout ── */}
                <div className="settings-section">
                    <button className="btn btn-danger btn-block" onClick={handleLogout} disabled={isLoggingOut}>
                        {isLoggingOut ? t('settings.loggingOut') : t('settings.logOut')}
                    </button>
                </div>

                {/* ── About ── */}
                <div className="settings-section settings-about">
                    <div className="settings-about-header">
                        <h5>{t('settings.aboutTitle')}</h5>
                        <span className="text-muted">{t('settings.version', { version: APP_VERSION })}</span>
                    </div>
                    <p>{t('settings.aboutDescription')}</p>
                    <ul className="settings-about-features">
                        <li>{t('settings.feature1')}</li>
                        <li>{t('settings.feature2')}</li>
                        <li>{t('settings.feature3')}</li>
                        <li>{t('settings.feature4')}</li>
                    </ul>
                    <a href="https://github.com/MrPix/Briefcase" target="_blank" rel="noopener noreferrer">
                        {t('settings.githubRepo')}
                    </a>
                </div>
            </div>
        </div>
    )
}
