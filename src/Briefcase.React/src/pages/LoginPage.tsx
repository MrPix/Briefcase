import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthException, useAuth } from '../auth/AuthContext'
import { deviceInfo } from '../auth/deviceInfo'
import { devicesApi } from '../services/devices'
import { TransferIcon } from '../components/icons'

export function LoginPage() {
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
            setError(err instanceof AuthException ? err.message : 'Unable to connect to the server. Please try again later.')
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
            setLoginCodeError(`Couldn't start device sign-in: ${err instanceof Error ? err.message : String(err)}`)
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
                setLoginCodeError('This code expired. Please generate a new one.')
                setShowLoginCode(false)
                setLoginCode(null)
            }
        } catch (err) {
            if ((err as DOMException)?.name === 'AbortError') return
            setLoginCodeError(`Device sign-in failed: ${err instanceof Error ? err.message : String(err)}`)
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
                <h2 className="auth-title">Sign In</h2>
                <p className="auth-subtitle">Welcome back! Sign in to your account.</p>

                {error && <div className="alert alert-danger">{error}</div>}

                <form onSubmit={handleLogin}>
                    <div className="mb-3">
                        <label htmlFor="email" className="form-label">
                            Email
                        </label>
                        <input
                            id="email"
                            type="email"
                            className="form-control"
                            placeholder="you@example.com"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">
                            Password
                        </label>
                        <input
                            id="password"
                            type="password"
                            className="form-control"
                            placeholder="Enter your password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </div>

                    <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                        {loading ? <span className="spinner" /> : null}
                        {loading ? ' Signing in…' : 'Sign In'}
                    </button>
                </form>

                <div className="auth-divider">
                    <span>or</span>
                </div>

                {externalProviders.map((provider) => (
                    <button
                        key={provider.key}
                        type="button"
                        className="btn btn-outline btn-block auth-social-btn"
                        onClick={() => startExternalLogin(provider.key)}
                        disabled={loading}
                    >
                        Continue with {provider.displayName}
                    </button>
                ))}

                <div className="auth-footer">
                    <span>Don't have an account?</span>
                    <Link to="/signup">Sign Up</Link>
                </div>

                <div className="auth-footer auth-receive-row">
                    <span>Receiving a file from another device?</span>
                    <Link to="/transfer" className="auth-receive-link">
                        <TransferIcon size={14} /> Receive File
                    </Link>
                </div>

                <div className="auth-divider">
                    <span>or</span>
                </div>

                {!showLoginCode ? (
                    <button
                        type="button"
                        className="btn btn-outline btn-block auth-social-btn"
                        onClick={startLoginByCode}
                        disabled={loading || generatingCode}
                    >
                        {generatingCode ? 'Preparing…' : 'Add this device with a code'}
                    </button>
                ) : (
                    <div className="login-code-panel">
                        <p>
                            On a device you're already signed in on, open <strong>Devices</strong>, choose{' '}
                            <strong>Add device</strong>, and enter this code:
                        </p>
                        <div className="login-code" aria-label="Login code">
                            {loginCode}
                        </div>
                        <p className="text-muted">Waiting for approval…</p>
                        {loginCodeError && <div className="alert alert-danger">{loginCodeError}</div>}
                        <button type="button" className="btn btn-outline" onClick={cancelLoginByCode}>
                            Cancel
                        </button>
                    </div>
                )}
            </div>
        </div>
    )
}
