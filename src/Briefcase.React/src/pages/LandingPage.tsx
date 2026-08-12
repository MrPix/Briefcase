import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { BrandLogo } from '../components/BrandLogo'
import { ClipboardIcon, LockIcon, DevicesIcon, TransferIcon, FileIcon, LinkIcon } from '../components/icons'

const FEATURES = [
    { icon: ClipboardIcon, title: 'Your Briefcase', text: 'Keep notes, links, and files together, ready wherever you need them.' },
    { icon: LockIcon, title: 'End-to-End Encrypted', text: 'Your messages are encrypted client-side. Not even the server can read your content.' },
    { icon: DevicesIcon, title: 'All Your Devices', text: 'Native apps for Windows, macOS, Android, and iOS — plus a Progressive Web App for everything else.' },
    { icon: TransferIcon, title: 'Instant QR Transfer', text: 'Send text or files to a nearby device in seconds — no account needed on the receiving end.' },
    { icon: FileIcon, title: 'File Attachments', text: 'Upload files up to 100 MB and access them from any device — streamed securely through the API.' },
    { icon: LinkIcon, title: 'Shareable Links', text: 'Generate a public link for any note or file. Set it to expire or self-destruct after one view.' },
]

export function LandingPage() {
    const { isAuthenticated } = useAuth()
    if (isAuthenticated) return <Navigate to="/clipboard" replace />

    return (
        <div className="landing-page">
            <section className="landing-hero">
                <div className="landing-hero-inner">
                    <div className="landing-logo">
                        <BrandLogo size={56} />
                        <h1 className="landing-title">Briefcase</h1>
                    </div>

                    <p className="landing-tagline">Your stuff. Everywhere.</p>
                    <p className="landing-description">
                        Put notes, links, and files in your Briefcase on one device. Find them on every other device, protected
                        with end-to-end encryption.
                    </p>

                    <div className="landing-cta-primary">
                        <Link className="btn btn-primary btn-lg landing-btn" to="/login">
                            Sign In
                        </Link>
                        <Link className="btn btn-outline btn-lg landing-btn" to="/signup">
                            Create Account
                        </Link>
                    </div>

                    <div className="landing-cta-secondary">
                        <Link className="btn btn-outline landing-btn-secondary" to="/transfer">
                            <TransferIcon size={18} /> Receive File
                        </Link>
                    </div>
                </div>
            </section>

            <section className="landing-features">
                <div className="landing-features-inner">
                    <h2 className="landing-features-title">Everything in one place</h2>
                    <div className="landing-features-grid">
                        {FEATURES.map(({ icon: Icon, title, text }) => (
                            <div className="landing-feature-card" key={title}>
                                <div className="landing-feature-icon">
                                    <Icon size={28} />
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
