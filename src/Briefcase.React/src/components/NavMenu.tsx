import { NavLink } from 'react-router-dom'
import { BrandLogo } from './BrandLogo'
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
} from './icons'

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

            <div className="sidebar-section-label">Contents</div>
            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/clipboard" end>
                    <ClipboardIcon />
                    <span>All</span>
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
                    <span>Notes</span>
                </NavLink>
            </nav>

            <div className="sidebar-divider" />

            <div className="sidebar-section-label">Devices</div>
            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/devices">
                    <DevicesIcon />
                    <span>Devices</span>
                </NavLink>
                <NavLink className={linkClass} to="/transfer">
                    <TransferIcon />
                    <span>Receive</span>
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
            </nav>

        </div>
    )
}
