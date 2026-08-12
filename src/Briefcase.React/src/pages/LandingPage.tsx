import { Link, Navigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'
import { BrandLogo } from '../components/BrandLogo'
import { SyncIllustration } from '../components/SyncIllustration'
import { ClipboardIcon, LockIcon, DevicesIcon, TransferIcon, FileIcon, LinkIcon } from '../components/icons'

const FEATURES = [
    { icon: ClipboardIcon, key: 'briefcase' },
    { icon: LockIcon, key: 'encrypted' },
    { icon: DevicesIcon, key: 'devices' },
    { icon: TransferIcon, key: 'transfer' },
    { icon: FileIcon, key: 'files' },
    { icon: LinkIcon, key: 'links' },
] as const

export function LandingPage() {
    const { t } = useTranslation()
    const { isAuthenticated } = useAuth()
    if (isAuthenticated) return <Navigate to="/clipboard" replace />

    return (
        <div className="landing-page">
            <section className="landing-hero">
                <div className="landing-hero-inner">
                    <div className="landing-logo">
                        <BrandLogo size={44} />
                        <h1 className="landing-title">Briefcase</h1>
                    </div>

                    <p className="landing-tagline">{t('landing.tagline')}</p>
                    <p className="landing-description">{t('landing.description')}</p>

                    <div className="landing-illustration">
                        <SyncIllustration />
                    </div>

                    <div className="landing-cta-primary">
                        <Link className="btn btn-primary btn-lg landing-btn" to="/signup">
                            {t('landing.createAccount')}
                        </Link>
                        <Link className="btn btn-outline btn-lg landing-btn" to="/login">
                            {t('landing.signIn')}
                        </Link>
                    </div>

                    <div className="landing-cta-secondary">
                        <Link className="landing-link-secondary" to="/transfer">
                            <TransferIcon size={16} /> {t('landing.receiveFile')}
                        </Link>
                        <p className="landing-cta-hint">{t('landing.receiveFileHint')}</p>
                    </div>
                </div>
            </section>

            <section className="landing-features">
                <div className="landing-features-inner">
                    <h2 className="landing-features-title">{t('landing.featuresTitle')}</h2>
                    <div className="landing-features-grid">
                        {FEATURES.map(({ icon: Icon, key }) => (
                            <div className="landing-feature-card" key={key}>
                                <div className="landing-feature-icon">
                                    <Icon size={24} />
                                </div>
                                <h3>{t(`landing.features.${key}.title`)}</h3>
                                <p>{t(`landing.features.${key}.text`)}</p>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            <footer className="landing-footer">
                <p>
                    {t('landing.openSource')}{' '}
                    <a href="https://github.com/MrPix/Briefcase" target="_blank" rel="noopener noreferrer">
                        {t('landing.github')}
                    </a>
                </p>
            </footer>
        </div>
    )
}
