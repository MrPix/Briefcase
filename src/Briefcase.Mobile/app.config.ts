import type { ExpoConfig, ConfigContext } from 'expo/config'

// Absolute base URL of the Briefcase API. React Native cannot use relative URLs,
// so this must point at a reachable host:
//   Android emulator : http://10.0.2.2:<apiPort>
//   iOS simulator    : http://localhost:<apiPort>
//   Physical device  : http://<your-LAN-IP>:<apiPort>
// Override via the EXPO_PUBLIC_API_BASE_URL environment variable.
const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://10.0.2.2:5218'

export default ({ config }: ConfigContext): ExpoConfig => ({
    ...config,
    name: 'Briefcase',
    slug: 'briefcase-mobile',
    version: '1.0.0',
    orientation: 'portrait',
    icon: './assets/icon.png',
    scheme: 'briefcase',
    userInterfaceStyle: 'automatic',
    ios: {
        supportsTablet: true,
        bundleIdentifier: 'com.briefcase.mobile',
        // Allow cleartext HTTP to the dev API host during development.
        infoPlist: {
            NSAppTransportSecurity: {
                NSAllowsArbitraryLoads: true,
            },
        },
    },
    android: {
        package: 'com.briefcase.mobile',
        adaptiveIcon: {
            backgroundColor: '#0EA5E9',
            foregroundImage: './assets/android-icon-foreground.png',
            backgroundImage: './assets/android-icon-background.png',
            monochromeImage: './assets/android-icon-monochrome.png',
        },
        predictiveBackGestureEnabled: false,
    },
    web: {
        favicon: './assets/favicon.png',
    },
    plugins: ['expo-router', 'expo-secure-store', 'expo-web-browser'],
    extra: {
        apiBaseUrl: API_BASE_URL,
    },
})
