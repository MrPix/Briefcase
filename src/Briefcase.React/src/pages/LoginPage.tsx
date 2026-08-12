import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Trans, useTranslation } from 'react-i18next'
import { AuthException, useAuth } from '../auth/AuthContext'
import { deviceInfo } from '../auth/deviceInfo'
import { devicesApi } from '../services/devices'
import { TransferIcon } from '../components/icons'

export function LoginPage() {
    const { t } = useTranslation()
    const { login, externalProviders, buildExternalLoginUrl, completeExternalLogin } = useAuth()
    const navigate = useNavigate()

    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState(false)

    const [showLoginCode, setShowLoginCode] = useState(false)
    const [generatingCode, setGeneratingCode] = useState(false)
    const [loginCode, setLoginCode] = useState<string | null>(null)
    const [loginCodeError, setLoginCodeError] = useState<string | null>(null)
    const abortRef = useRef<AbortController | null>(null)
    const handledFragment = useRef(false)

    // Complete OAuth login when redirected back with tokens in the URL fragment.
    useEffect(() => {
        if (handledFragment.current) return
        handledFragment.current = true

        const hash = window.location.hash.replace(/^#/, '')
        if (!hash) return
        const params = new URLSearchParams(hash)
        const accessToken = params.get('access_token')
        const refreshToken = params.get('refresh_token')
        const expiresAt = params.get('access_token_expires_at')
        if (accessToken && refreshToken && expiresAt) {
            completeExternalLogin(accessToken, refreshToken, expiresAt)
            window.history.replaceState(null, '', window.location.pathname)
            navigate('/clipboard', { replace: true })
        }
    }, [completeExternalLogin, navigate])

    useEffect(() => () => abortRef.current?.abort(), [])

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault()
        setLoading(true)
        setError(null)
        try {
            await login(email, password)
            navigate('/clipboard')
        } catch (err) {
            setError(err instanceof AuthException ? err.message : t('login.connectionError'))
        } finally {
            setLoading(false)
        }
    }

    const startExternalLogin = (provider: string) => {
        const redirectUri = `${window.location.origin}/login`
        window.location.href = buildExternalLoginUrl(provider, redirectUri)
    }

    const startLoginByCode = async () => {
        setGeneratingCode(true)
        setLoginCodeError(null)
        setError(null)
        try {
            const info = await devicesApi.generateLoginCode(deviceInfo.deviceName, deviceInfo.platform)
            setLoginCode(info.code)
            setShowLoginCode(true)
            abortRef.current?.abort()
            abortRef.current = new AbortController()
            waitForApproval(info.code, abortRef.current.signal)
        } catch (err) {
            setLoginCodeError(t('login.deviceSignInStartFailed', { error: err instanceof Error ? err.message : String(err) }))
        } finally {
            setGeneratingCode(false)
        }
    }

    const waitForApproval = async (code: string, signal: AbortSignal) => {
        try {
            const result = await devicesApi.waitForLoginApproval(code, signal)
            if (signal.aborted) return
            if (result.status === 'approved' && result.accessToken && result.refreshToken && result.accessTokenExpiresAt) {
                completeExternalLogin(result.accessToken, result.refreshToken, result.accessTokenExpiresAt)
                navigate('/clipboard', { replace: true })
            } else {
                setLoginCodeError(t('login.codeExpired'))
                setShowLoginCode(false)
                setLoginCode(null)
            }
        } catch (err) {
            if ((err as DOMException)?.name === 'AbortError') return
            setLoginCodeError(t('login.deviceSignInFailed', { error: err instanceof Error ? err.message : String(err) }))
        }
    }

    const cancelLoginByCode = () => {
        abortRef.current?.abort()
        setShowLoginCode(false)
        setLoginCode(null)
        setLoginCodeError(null)
    }

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h2 className="auth-title">{t('login.title')}</h2>
                <p className="auth-subtitle">{t('login.subtitle')}</p>

                {error && <div className="alert alert-danger">{error}</div>}

                <form onSubmit={handleLogin}>
                    <div className="mb-3">
                        <label htmlFor="email" className="form-label">
                            {t('login.emailLabel')}
                        </label>
                        <input
                            id="email"
                            type="email"
                            className="form-control"
                            placeholder={t('login.emailPlaceholder')}
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">
                            {t('login.passwordLabel')}
                        </label>
                        <input
                            id="password"
                            type="password"
                            className="form-control"
                            placeholder={t('login.passwordPlaceholder')}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </div>

                    <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                        {loading ? <span className="spinner" /> : null}
                        {loading ? ` ${t('login.signingIn')}` : t('login.signIn')}
                    </button>
                </form>

                <div className="auth-divider">
                    <span>{t('login.or')}</span>
                </div>

                {externalProviders.map((provider) => (
                    <button
                        key={provider.key}
                        type="button"
                        className="btn btn-outline btn-block auth-social-btn"
                        onClick={() => startExternalLogin(provider.key)}
                        disabled={loading}
                    >
                        {t('login.continueWith', { provider: provider.displayName })}
                    </button>
                ))}

                <div className="auth-footer">
                    <span>{t('login.noAccount')}</span>
                    <Link to="/signup">{t('login.signUp')}</Link>
                </div>

                <div className="auth-footer auth-receive-row">
                    <span>{t('login.receivingFile')}</span>
                    <Link to="/transfer" className="auth-receive-link">
                        <TransferIcon size={14} /> {t('login.receiveFile')}
                    </Link>
                </div>

                <div className="auth-divider">
                    <span>{t('login.or')}</span>
                </div>

                {!showLoginCode ? (
                    <button
                        type="button"
                        className="btn btn-outline btn-block auth-social-btn"
                        onClick={startLoginByCode}
                        disabled={loading || generatingCode}
                    >
                        {generatingCode ? t('login.preparing') : t('login.addDeviceCode')}
                    </button>
                ) : (
                    <div className="login-code-panel">
                        <p>
                            <Trans i18nKey="login.addDeviceInstructions">
                                On a device you're already signed in on, open <strong>Devices</strong>, choose{' '}
                                <strong>Add device</strong>, and enter this code:
                            </Trans>
                        </p>
                        <div className="code-display-group login-code" aria-label="Login code">
                            <span className="code-half">{loginCode?.slice(0, 4)}</span>
                            <span className="code-separator" aria-hidden="true">–</span>
                            <span className="code-half">{loginCode?.slice(4)}</span>
                        </div>
                        <p className="text-muted">{t('login.waitingApproval')}</p>
                        {loginCodeError && <div className="alert alert-danger">{loginCodeError}</div>}
                        <button type="button" className="btn btn-outline" onClick={cancelLoginByCode}>
                            {t('common.cancel')}
                        </button>
                    </div>
                )}
            </div>
        </div>
    )
}
