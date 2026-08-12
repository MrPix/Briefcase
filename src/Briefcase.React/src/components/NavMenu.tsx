import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
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
    const { t } = useTranslation()
    return (
        <div className="sidebar-nav">
            <div className="sidebar-header">
                <div className="sidebar-brand">
                    <BrandLogo />
                    <span className="brand-text">Briefcase</span>
                </div>
            </div>

            <div className="sidebar-section-label">{t('nav.contentsSection')}</div>
            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/clipboard" end>
                    <ClipboardIcon />
                    <span>{t('nav.all')}</span>
                </NavLink>
                <NavLink className={linkClass} to="/favorites">
                    <StarIcon />
                    <span>{t('nav.favorites')}</span>
                </NavLink>
                <NavLink className={linkClass} to="/files">
                    <FileIcon />
                    <span>{t('nav.files')}</span>
                </NavLink>
                <NavLink className={linkClass} to="/links">
                    <LinkIcon />
                    <span>{t('nav.links')}</span>
                </NavLink>
                <NavLink className={linkClass} to="/text">
                    <TextIcon />
                    <span>{t('nav.notes')}</span>
                </NavLink>
            </nav>

            <div className="sidebar-divider" />

            <div className="sidebar-section-label">{t('nav.devicesSection')}</div>
            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/devices">
                    <DevicesIcon />
                    <span>{t('nav.devices')}</span>
                </NavLink>
                <NavLink className={linkClass} to="/transfer">
                    <TransferIcon />
                    <span>{t('nav.receive')}</span>
                </NavLink>
            </nav>

            <div className="sidebar-divider" />

            <nav className="sidebar-links">
                <NavLink className={linkClass} to="/trash">
                    <TrashIcon />
                    <span>{t('nav.trash')}</span>
                </NavLink>
                <NavLink className={linkClass} to="/settings">
                    <SettingsIcon />
                    <span>{t('nav.settings')}</span>
                </NavLink>
            </nav>

        </div>
    )
}
