import { registerSW } from 'virtual:pwa-register'

// Reload the page once the new service worker takes control so a redeploy is
// applied automatically — no manual hard refresh (critical on mobile).
if ('serviceWorker' in navigator) {
    let refreshing = false
    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (refreshing) return
        refreshing = true
        window.location.reload()
    })
}

registerSW({
    immediate: true,
    onRegisteredSW(_swUrl, registration) {
        // Re-check for a newer deploy hourly for long-lived tabs / installed PWAs.
        if (registration) {
            setInterval(() => registration.update(), 60 * 60 * 1000)
        }
    },
})
