import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { NavMenu } from './NavMenu'

/** Protected shell: sidebar + routed content. Redirects to /login when signed out. */
export function AppLayout() {
    const { isAuthenticated, restoring } = useAuth()
    const location = useLocation()

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
        <div className="page">
            <div className="sidebar">
                <NavMenu />
            </div>
            <main className="page-main">
                <Outlet />
            </main>
        </div>
    )
}
