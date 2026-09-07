import { useEffect, useRef, useState } from 'react'
import {
    KeyboardAvoidingView,
    Platform,
    ScrollView,
    StyleSheet,
    Text,
    TextInput,
    View,
} from 'react-native'
import { Link, useRouter } from 'expo-router'
import { AuthException, useAuth } from '@/auth/AuthContext'
import { deviceInfo } from '@/auth/deviceInfo'
import { devicesApi } from '@/services/devices'
import { Button } from '@/ui/Button'
import { colors, sharedStyles, spacing } from '@/ui/theme'

export default function LoginScreen() {
    const { login, externalProviders, loginWithProvider, completeExternalLogin } = useAuth()
    const router = useRouter()

    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState(false)

    const [showLoginCode, setShowLoginCode] = useState(false)
    const [generatingCode, setGeneratingCode] = useState(false)
    const [loginCode, setLoginCode] = useState<string | null>(null)
    const [loginCodeError, setLoginCodeError] = useState<string | null>(null)
    const abortRef = useRef<AbortController | null>(null)

    useEffect(() => () => abortRef.current?.abort(), [])

    const handleLogin = async () => {
        setLoading(true)
        setError(null)
        try {
            await login(email.trim(), password)
            router.replace('/(app)/clipboard')
        } catch (err) {
            setError(err instanceof AuthException ? err.message : 'Unable to connect. Try again.')
        } finally {
            setLoading(false)
        }
    }

    const handleProvider = async (provider: string) => {
        setError(null)
        try {
            await loginWithProvider(provider)
            router.replace('/(app)/clipboard')
        } catch (err) {
            setError(err instanceof AuthException ? err.message : 'Sign-in failed.')
        }
    }

    const startLoginByCode = async () => {
        setGeneratingCode(true)
        setLoginCodeError(null)
        setError(null)
        try {
            const info = await devicesApi.generateLoginCode(deviceInfo.deviceName, deviceInfo.platform)
            setLoginCode(info.code)
            setShowLoginCode(true)
            abortRef.current?.abort()
            abortRef.current = new AbortController()
            waitForApproval(info.code, abortRef.current.signal)
        } catch (err) {
            setLoginCodeError(err instanceof Error ? err.message : String(err))
        } finally {
            setGeneratingCode(false)
        }
    }

    const waitForApproval = async (code: string, signal: AbortSignal) => {
        try {
            const result = await devicesApi.waitForLoginApproval(code, signal)
            if (signal.aborted) return
            if (
                result.status === 'approved' &&
                result.accessToken &&
                result.refreshToken &&
                result.accessTokenExpiresAt
            ) {
                completeExternalLogin(result.accessToken, result.refreshToken, result.accessTokenExpiresAt)
                router.replace('/(app)/clipboard')
            } else {
                setLoginCodeError('The code expired. Please try again.')
                setShowLoginCode(false)
                setLoginCode(null)
            }
        } catch (err) {
            if ((err as { name?: string })?.name === 'AbortError') return
            setLoginCodeError(err instanceof Error ? err.message : String(err))
        }
    }

    const cancelLoginByCode = () => {
        abortRef.current?.abort()
        setShowLoginCode(false)
        setLoginCode(null)
        setLoginCodeError(null)
    }

    return (
        <KeyboardAvoidingView
            style={sharedStyles.screen}
            behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        >
            <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
                <Text style={styles.brand}>Briefcase</Text>
                <Text style={sharedStyles.title}>Welcome back</Text>
                <Text style={[sharedStyles.subtitle, styles.gapBottom]}>
                    Sign in to sync your clipboard across devices.
                </Text>

                {error && <Text style={[sharedStyles.errorText, styles.gapBottom]}>{error}</Text>}

                <View style={styles.field}>
                    <Text style={sharedStyles.label}>Email</Text>
                    <TextInput
                        style={sharedStyles.input}
                        placeholder="you@example.com"
                        placeholderTextColor={colors.textMuted}
                        autoCapitalize="none"
                        keyboardType="email-address"
                        autoComplete="email"
                        value={email}
                        onChangeText={setEmail}
                    />
                </View>

                <View style={styles.field}>
                    <Text style={sharedStyles.label}>Password</Text>
                    <TextInput
                        style={sharedStyles.input}
                        placeholder="••••••••"
                        placeholderTextColor={colors.textMuted}
                        secureTextEntry
                        value={password}
                        onChangeText={setPassword}
                    />
                </View>

                <Button title="Sign in" onPress={handleLogin} loading={loading} />

                <View style={styles.divider}>
                    <View style={styles.line} />
                    <Text style={styles.dividerText}>or</Text>
                    <View style={styles.line} />
                </View>

                {externalProviders.map((provider) => (
                    <View key={provider.key} style={styles.field}>
                        <Button
                            title={`Continue with ${provider.displayName}`}
                            variant="outline"
                            onPress={() => handleProvider(provider.key)}
                        />
                    </View>
                ))}

                {!showLoginCode ? (
                    <Button
                        title={generatingCode ? 'Preparing…' : 'Add this device with a code'}
                        variant="outline"
                        loading={generatingCode}
                        onPress={startLoginByCode}
                    />
                ) : (
                    <View style={styles.codePanel}>
                        <Text style={sharedStyles.subtitle}>
                            On a signed-in device, open Devices → Add device and enter this code:
                        </Text>
                        <Text style={styles.code}>
                            {loginCode?.slice(0, 4)} – {loginCode?.slice(4)}
                        </Text>
                        <Text style={sharedStyles.subtitle}>Waiting for approval…</Text>
                        {loginCodeError && <Text style={sharedStyles.errorText}>{loginCodeError}</Text>}
                        <Button title="Cancel" variant="outline" onPress={cancelLoginByCode} />
                    </View>
                )}

                <View style={styles.footer}>
                    <Text style={sharedStyles.subtitle}>No account? </Text>
                    <Link href="/signup" style={styles.linkText}>
                        Sign up
                    </Link>
                </View>
            </ScrollView>
        </KeyboardAvoidingView>
    )
}

const styles = StyleSheet.create({
    container: {
        padding: spacing.lg,
        gap: spacing.md,
        flexGrow: 1,
        justifyContent: 'center',
    },
    brand: {
        color: colors.accentSoft,
        fontSize: 18,
        fontWeight: '700',
        marginBottom: spacing.sm,
    },
    gapBottom: {
        marginBottom: spacing.sm,
    },
    field: {
        gap: spacing.xs,
    },
    divider: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: spacing.sm,
    },
    line: {
        flex: 1,
        height: 1,
        backgroundColor: colors.border,
    },
    dividerText: {
        color: colors.textMuted,
    },
    codePanel: {
        gap: spacing.sm,
        padding: spacing.md,
        borderRadius: 12,
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: colors.surface,
        alignItems: 'center',
    },
    code: {
        color: colors.text,
        fontSize: 28,
        fontWeight: '700',
        letterSpacing: 4,
    },
    footer: {
        flexDirection: 'row',
        justifyContent: 'center',
        alignItems: 'center',
        marginTop: spacing.sm,
    },
    linkText: {
        color: colors.accentSoft,
        fontWeight: '600',
        fontSize: 15,
    },
})
