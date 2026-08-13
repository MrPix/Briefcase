/** Best-effort browser/device identification for the auth device registration. */
function detectBrowser(): string {
    const ua = navigator.userAgent
    if (/Edg\//.test(ua)) return 'Edge'
    if (/OPR\//.test(ua)) return 'Opera'
    if (/Chrome\//.test(ua)) return 'Chrome'
    if (/Firefox\//.test(ua)) return 'Firefox'
    if (/Safari\//.test(ua)) return 'Safari'
    return 'Browser'
}

function detectDevice(): string | null {
    const ua = navigator.userAgent
    if (/iPhone/.test(ua)) return 'iPhone'
    if (/iPad/.test(ua)) return 'iPad'

    const android = ua.match(/Android [^;]+;\s*([^;)]+?)\s+Build\//)
    if (android?.[1]) return android[1].trim()
    if (/Android/.test(ua)) return 'Android'
    if (/Windows/.test(ua)) return 'Windows'
    if (/CrOS/.test(ua)) return 'ChromeOS'
    if (/Mac OS X/.test(ua)) return 'macOS'
    if (/Linux/.test(ua)) return 'Linux'
    return null
}

function detectDeviceName(): string {
    const browser = detectBrowser()
    const device = detectDevice()
    return device ? `${browser} on ${device}` : `${browser} (Web)`
}

const INSTALLATION_ID_KEY = 'briefcase_installation_id'

/** Stable per-install id so the server can bind sessions to this browser profile. */
function getInstallationId(): string {
    let id = localStorage.getItem(INSTALLATION_ID_KEY)
    if (!id) {
        id = crypto.randomUUID()
        localStorage.setItem(INSTALLATION_ID_KEY, id)
    }
    return id
}

export const deviceInfo = {
    deviceName: detectDeviceName(),
    // Server expects the platform as a string here (matches IDeviceInfoProvider.Platform).
    platform: 'Web',
    installationId: getInstallationId(),
}
