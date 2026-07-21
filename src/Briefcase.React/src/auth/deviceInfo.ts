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

export const deviceInfo = {
    deviceName: `${detectBrowser()} (Web)`,
    // Server expects the platform as a string here (matches IDeviceInfoProvider.Platform).
    platform: 'Web',
}
