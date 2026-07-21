import { NavLink } from 'react-router-dom'
import {
    ClipboardIcon,
    StarIcon,
    FileIcon,
    LinkIcon,
    TextIcon,
    DevicesIcon,
    TransferIcon,
    TrashIcon,
    SettingsIcon,
    InfoIcon,
    LockIcon,
} from './icons'

export function BrandLogo() {
    return (
        <svg width="28" height="28" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">
            <defs>
                <linearGradient id="briefcaseBrandBg" x1="2" y1="2" x2="26" y2="26" gradientUnits="userSpaceOnUse">
                    <stop offset="0" stopColor="#22C55E" />
                    <stop offset="1" stopColor="#0EA5E9" />
                </linearGradient>
            </defs>
            <rect width="28" height="28" rx="8" fill="url(#briefcaseBrandBg)" />
            <path
                d="M9 6.5V20.9M9 6.5H13.9C16.9 6.5 18.8 8.3 18.8 10.8C18.8 13.2 16.9 15 13.9 15H9M9 15H14.5C17.7 15 19.6 16.9 19.6 19.3C19.6 21.8 17.7 23.5 14.5 23.5H9"
                stroke="white"
                strokeWidth="2.4"
                strokeLinecap="round"
                strokeLinejoin="round"
            />
            <rect x="15.6" y="12.3" width="3.1" height="3.1" rx="0.95" fill="#F59E0B" transform="rotate(12 17.15 13.85)" />
        </svg>
    )
}

const linkClass = ({ isActive }: { isActive: boolean }) => `sidebar-link${isActive ? ' active' : ''}`

export function NavMenu() {
    return (
        <div className="sidebar-nav">
            <div className="sidebar-header">
                <div className="sidebar-brand">
                    <BrandLogo />
                    <span className="brand-text">Briefcase</span>
                </div>
            </div>

            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/clipboard" end>
                    <ClipboardIcon />
                    <span>All Messages</span>
                </NavLink>
                <NavLink className={linkClass} to="/favorites">
                    <StarIcon />
                    <span>Favorites</span>
                </NavLink>
                <NavLink className={linkClass} to="/files">
                    <FileIcon />
                    <span>Files</span>
                </NavLink>
                <NavLink className={linkClass} to="/links">
                    <LinkIcon />
                    <span>Links</span>
                </NavLink>
                <NavLink className={linkClass} to="/text">
                    <TextIcon />
                    <span>Text</span>
                </NavLink>
            </nav>

            <div className="sidebar-divider" />

            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/devices">
                    <DevicesIcon />
                    <span>Devices</span>
                </NavLink>
                <NavLink className={linkClass} to="/transfer">
                    <TransferIcon />
                    <span>QR Transfer</span>
                </NavLink>
            </nav>

            <div className="sidebar-divider" />

            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/trash">
                    <TrashIcon />
                    <span>Trash</span>
                </NavLink>
                <NavLink className={linkClass} to="/settings">
                    <SettingsIcon />
                    <span>Settings</span>
                </NavLink>
                <NavLink className={linkClass} to="/about">
                    <InfoIcon />
                    <span>About</span>
                </NavLink>
            </nav>

            <div className="sidebar-footer">
                <div className="encryption-status">
                    <LockIcon size={16} style={{ stroke: 'var(--green)' }} />
                    <div>
                        <span className="footer-label">End-to-End Encryption</span>
                        <span className="footer-value">Client-side</span>
                    </div>
                </div>
            </div>
        </div>
    )
}
