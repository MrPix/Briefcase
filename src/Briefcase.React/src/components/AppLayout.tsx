import { useEffect, useState } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { BrandLogo } from './BrandLogo'
import { NavMenu } from './NavMenu'
import { MenuIcon } from './icons'

/** Protected shell: sidebar + routed content. Redirects to /login when signed out. */
export function AppLayout() {
    const { isAuthenticated, restoring } = useAuth()
    const location = useLocation()
    const [navOpen, setNavOpen] = useState(false)

    // Close the mobile drawer whenever the route changes.
    useEffect(() => {
        setNavOpen(false)
    }, [location.pathname])

    if (restoring) {
        return (
            <div className="app-loading">
                <span className="spinner" /> Loading…
            </div>
        )
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" replace state={{ from: location }} />
    }

    return (
        <div className={`page${navOpen ? ' nav-open' : ''}`}>
            <header className="mobile-topbar">
                <button
                    type="button"
                    className="mobile-nav-toggle"
                    aria-label="Open navigation menu"
                    aria-expanded={navOpen}
                    onClick={() => setNavOpen(true)}
                >
                    <MenuIcon />
                </button>
                <div className="mobile-topbar-brand">
                    <BrandLogo />
                    <span className="brand-text">Briefcase</span>
                </div>
            </header>

            <div className="nav-scrim" onClick={() => setNavOpen(false)} aria-hidden="true" />

            <div className="sidebar">
                <NavMenu />
            </div>
            <main className="page-main">
                <Outlet />
            </main>
        </div>
    )
}
