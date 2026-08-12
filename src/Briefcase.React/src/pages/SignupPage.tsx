import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AuthException, useAuth } from '../auth/AuthContext'

export function SignupPage() {
    const { t } = useTranslation()
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
            setError(t('signup.passwordMismatch'))
            return
        }
        if (password.length < 6) {
            setError(t('signup.passwordTooShort'))
            return
        }

        setLoading(true)
        try {
            await register(email, password, displayName)
            navigate('/clipboard')
        } catch (err) {
            setError(
                err instanceof AuthException ? err.message : t('signup.registrationFailed'),
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
                <h2 className="auth-title">{t('signup.title')}</h2>
                <p className="auth-subtitle">{t('signup.subtitle')}</p>

                {error && <div className="alert alert-danger">{error}</div>}

                <form onSubmit={handleSignup}>
                    <div className="mb-3">
                        <label htmlFor="displayName" className="form-label">
                            {t('signup.displayNameLabel')}
                        </label>
                        <input
                            id="displayName"
                            className="form-control"
                            placeholder={t('signup.displayNamePlaceholder')}
                            value={displayName}
                            onChange={(e) => setDisplayName(e.target.value)}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="email" className="form-label">
                            {t('signup.emailLabel')}
                        </label>
                        <input
                            id="email"
                            type="email"
                            className="form-control"
                            placeholder={t('signup.emailPlaceholder')}
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">
                            {t('signup.passwordLabel')}
                        </label>
                        <input
                            id="password"
                            type="password"
                            className="form-control"
                            placeholder={t('signup.passwordPlaceholder')}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="confirmPassword" className="form-label">
                            {t('signup.confirmPasswordLabel')}
                        </label>
                        <input
                            id="confirmPassword"
                            type="password"
                            className="form-control"
                            placeholder={t('signup.confirmPasswordPlaceholder')}
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            required
                        />
                    </div>

                    <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                        {loading ? <span className="spinner" /> : null}
                        {loading ? ` ${t('signup.creatingAccount')}` : t('signup.signUp')}
                    </button>
                </form>

                <div className="auth-divider">
                    <span>{t('signup.or')}</span>
                </div>

                {externalProviders.map((provider) => (
                    <button
                        key={provider.key}
                        type="button"
                        className="btn btn-outline btn-block auth-social-btn"
                        onClick={() => startExternalLogin(provider.key)}
                        disabled={loading}
                    >
                        {t('signup.continueWith', { provider: provider.displayName })}
                    </button>
                ))}

                <div className="auth-footer">
                    <span>{t('signup.haveAccount')}</span>
                    <Link to="/login">{t('signup.signIn')}</Link>
                </div>
            </div>
        </div>
    )
}
