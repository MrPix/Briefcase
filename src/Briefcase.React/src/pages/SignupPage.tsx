import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthException, useAuth } from '../auth/AuthContext'

export function SignupPage() {
    const { register, externalProviders, buildExternalLoginUrl, completeExternalLogin } = useAuth()
    const navigate = useNavigate()

    const [displayName, setDisplayName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState(false)
    const handledFragment = useRef(false)

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

    const handleSignup = async (e: React.FormEvent) => {
        e.preventDefault()
        setError(null)

        if (password !== confirmPassword) {
            setError('Passwords do not match.')
            return
        }
        if (password.length < 6) {
            setError('Password must be at least 6 characters.')
            return
        }

        setLoading(true)
        try {
            await register(email, password, displayName)
            navigate('/clipboard')
        } catch (err) {
            setError(
                err instanceof AuthException ? err.message : 'Registration failed. The email may already be in use.',
            )
        } finally {
            setLoading(false)
        }
    }

    const startExternalLogin = (provider: string) => {
        const redirectUri = `${window.location.origin}/signup`
        window.location.href = buildExternalLoginUrl(provider, redirectUri)
    }

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h2 className="auth-title">Create Account</h2>
                <p className="auth-subtitle">Sign up to get started.</p>

                {error && <div className="alert alert-danger">{error}</div>}

                <form onSubmit={handleSignup}>
                    <div className="mb-3">
                        <label htmlFor="displayName" className="form-label">
                            Display Name
                        </label>
                        <input
                            id="displayName"
                            className="form-control"
                            placeholder="Your name"
                            value={displayName}
                            onChange={(e) => setDisplayName(e.target.value)}
                            required
                        />
                    </div>

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
                            placeholder="Create a password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="confirmPassword" className="form-label">
                            Confirm Password
                        </label>
                        <input
                            id="confirmPassword"
                            type="password"
                            className="form-control"
                            placeholder="Confirm your password"
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            required
                        />
                    </div>

                    <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                        {loading ? <span className="spinner" /> : null}
                        {loading ? ' Creating account…' : 'Sign Up'}
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
                    <span>Already have an account?</span>
                    <Link to="/login">Sign In</Link>
                </div>
            </div>
        </div>
    )
}
