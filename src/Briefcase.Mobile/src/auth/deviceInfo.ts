import { Platform as RNPlatform } from 'react-native'
import * as Device from 'expo-device'
import * as SecureStore from 'expo-secure-store'
import * as Crypto from 'expo-crypto'

// Stable per-install identity so the server can bind sessions to this device.
// The platform string must match the server-side `Platform` enum names
// (Android / iOS / Windows / macOS / Web) — it is parsed via Enum.TryParse.

const INSTALLATION_ID_KEY = 'briefcase_installation_id'

function detectPlatform(): string {
    switch (RNPlatform.OS) {
        case 'android':
            return 'Android'
        case 'ios':
            return 'iOS'
        case 'macos':
            return 'macOS'
        case 'windows':
            return 'Windows'
        default:
            return 'Web'
    }
}

function detectDeviceName(): string {
    const model = Device.modelName ?? Device.deviceName ?? detectPlatform()
    return `${model} (${detectPlatform()})`
}

let cachedInstallationId: string | null = null

async function loadInstallationId(): Promise<string> {
    let id = await SecureStore.getItemAsync(INSTALLATION_ID_KEY)
    if (!id) {
        id = Crypto.randomUUID()
        await SecureStore.setItemAsync(INSTALLATION_ID_KEY, id)
    }
    cachedInstallationId = id
    return id
}

export const deviceInfo = {
    deviceName: detectDeviceName(),
    platform: detectPlatform(),
    get installationId(): string {
        return cachedInstallationId ?? ''
    },
    /** Must be awaited once at startup before authentication requests. */
    async load(): Promise<void> {
        await loadInstallationId()
    },
}
