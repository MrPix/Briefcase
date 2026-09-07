import { Stack } from 'expo-router'
import { StatusBar } from 'expo-status-bar'
import { SafeAreaProvider } from 'react-native-safe-area-context'
import { AuthProvider } from '@/auth/AuthContext'
import { colors } from '@/ui/theme'

export default function RootLayout() {
    return (
        <SafeAreaProvider>
            <AuthProvider>
                <StatusBar style="light" />
                <Stack
                    screenOptions={{
                        headerStyle: { backgroundColor: colors.bg },
                        headerTintColor: colors.text,
                        contentStyle: { backgroundColor: colors.bg },
                        headerShadowVisible: false,
                    }}
                >
                    <Stack.Screen name="index" options={{ headerShown: false }} />
                    <Stack.Screen name="login" options={{ headerShown: false }} />
                    <Stack.Screen name="signup" options={{ title: 'Create account' }} />
                    <Stack.Screen name="(app)" options={{ headerShown: false }} />
                </Stack>
            </AuthProvider>
        </SafeAreaProvider>
    )
}
