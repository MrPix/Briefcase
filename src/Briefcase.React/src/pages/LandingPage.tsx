import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { ClipboardIcon, LockIcon, DevicesIcon, TransferIcon, FileIcon, LinkIcon } from '../components/icons'

const FEATURES = [
    { icon: ClipboardIcon, title: 'Universal Clipboard', text: 'Copy anything on one device and paste it on another — text, links, and files all in one stream.' },
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
                        <svg width="56" height="56" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <defs>
                                <linearGradient id="landingBrandBg" x1="2" y1="2" x2="26" y2="26" gradientUnits="userSpaceOnUse">
                                    <stop offset="0" stopColor="#22C55E" />
                                    <stop offset="1" stopColor="#0EA5E9" />
                                </linearGradient>
                            </defs>
                            <rect width="28" height="28" rx="8" fill="url(#landingBrandBg)" />
                            <path
                                d="M9 6.5V20.9M9 6.5H13.9C16.9 6.5 18.8 8.3 18.8 10.8C18.8 13.2 16.9 15 13.9 15H9M9 15H14.5C17.7 15 19.6 16.9 19.6 19.3C19.6 21.8 17.7 23.5 14.5 23.5H9"
                                stroke="white"
                                strokeWidth="2.4"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                            <rect x="15.6" y="12.3" width="3.1" height="3.1" rx="0.95" fill="#F59E0B" transform="rotate(12 17.15 13.85)" />
                        </svg>
                        <h1 className="landing-title">Briefcase</h1>
                    </div>

                    <p className="landing-tagline">Your secure clipboard, everywhere.</p>
                    <p className="landing-description">
                        Save messages, links, and files on one device — access them instantly on all your others. End-to-end
                        encrypted, cross-platform, and always with you.
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
