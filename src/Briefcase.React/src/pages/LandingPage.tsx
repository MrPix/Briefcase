import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { BrandLogo } from '../components/BrandLogo'
import { SyncIllustration } from '../components/SyncIllustration'
import { ClipboardIcon, LockIcon, DevicesIcon, TransferIcon, FileIcon, LinkIcon } from '../components/icons'

const FEATURES = [
    { icon: ClipboardIcon, title: 'Your Briefcase', text: 'Notes, links, and files — all in one place, ready when you need them.' },
    { icon: LockIcon, title: 'End-to-End Encrypted', text: 'Private by design. Your content is encrypted before it ever leaves your device.' },
    { icon: DevicesIcon, title: 'All Your Devices', text: "Pick up right where you left off, on whichever device you're using." },
    { icon: TransferIcon, title: 'Instant QR Transfer', text: 'Send something to another device in seconds — just scan and go.' },
    { icon: FileIcon, title: 'File Attachments', text: 'Store files securely and open them from anywhere.' },
    { icon: LinkIcon, title: 'Shareable Links', text: 'Share exactly what you choose, without exposing the rest of your Briefcase.' },
]

export function LandingPage() {
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

                    <p className="landing-tagline">Your stuff. Everywhere.</p>
                    <p className="landing-description">
                        Put notes, links, and files in your Briefcase on one device. Find them on every other device, protected
                        with end-to-end encryption.
                    </p>

                    <div className="landing-illustration">
                        <SyncIllustration />
                    </div>

                    <div className="landing-cta-primary">
                        <Link className="btn btn-primary btn-lg landing-btn" to="/signup">
                            Create Account
                        </Link>
                        <Link className="btn btn-outline btn-lg landing-btn" to="/login">
                            Sign In
                        </Link>
                    </div>

                    <div className="landing-cta-secondary">
                        <Link className="landing-link-secondary" to="/transfer">
                            <TransferIcon size={16} /> Receive File
                        </Link>
                    </div>
                </div>
            </section>

            <section className="landing-features">
                <div className="landing-features-inner">
                    <h2 className="landing-features-title">Everything you need, everywhere you go</h2>
                    <div className="landing-features-grid">
                        {FEATURES.map(({ icon: Icon, title, text }) => (
                            <div className="landing-feature-card" key={title}>
                                <div className="landing-feature-icon">
                                    <Icon size={24} />
                                </div>
                                <h3>{title}</h3>
                                <p>{text}</p>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            <footer className="landing-footer">
                <p>
                    Open source &mdash;{' '}
                    <a href="https://github.com/MrPix/Briefcase" target="_blank" rel="noopener noreferrer">
                        GitHub
                    </a>
                </p>
            </footer>
        </div>
    )
}
