import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ThemeProvider } from './theme/ThemeContext'
import { AppLayout } from './components/AppLayout'
import { LandingPage } from './pages/LandingPage'
import { LoginPage } from './pages/LoginPage'
import { SignupPage } from './pages/SignupPage'
import { ClipboardPage } from './pages/ClipboardPage'
import { DevicesPage } from './pages/DevicesPage'
import { TransferPage } from './pages/TransferPage'
import { TrashPage } from './pages/TrashPage'
import { SettingsPage } from './pages/SettingsPage'
import { AboutPage } from './pages/AboutPage'
import { ShareViewPage } from './pages/ShareViewPage'

function App() {
    return (
        <ThemeProvider>
            <AuthProvider>
                <BrowserRouter>
                    <Routes>
                        {/* Public pages */}
                        <Route path="/" element={<LandingPage />} />
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/signup" element={<SignupPage />} />
                        <Route path="/transfer" element={<TransferPage />} />
                        <Route path="/share/:slug" element={<ShareViewPage />} />

                        {/* Protected pages (sidebar shell) */}
                        <Route element={<AppLayout />}>
                            <Route path="/clipboard" element={<ClipboardPage />} />
                            <Route path="/favorites" element={<ClipboardPage />} />
                            <Route path="/files" element={<ClipboardPage />} />
                            <Route path="/links" element={<ClipboardPage />} />
                            <Route path="/text" element={<ClipboardPage />} />
                            <Route path="/devices" element={<DevicesPage />} />
                            <Route path="/trash" element={<TrashPage />} />
                            <Route path="/settings" element={<SettingsPage />} />
                            <Route path="/about" element={<AboutPage />} />
                        </Route>

                        <Route path="*" element={<Navigate to="/" replace />} />
                    </Routes>
                </BrowserRouter>
            </AuthProvider>
        </ThemeProvider>
    )
}

export default App
