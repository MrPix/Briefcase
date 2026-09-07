import { useState } from 'react'
import {
    KeyboardAvoidingView,
    Platform,
    ScrollView,
    StyleSheet,
    Text,
    TextInput,
    View,
} from 'react-native'
import { useRouter } from 'expo-router'
import { AuthException, useAuth } from '@/auth/AuthContext'
import { Button } from '@/ui/Button'
import { colors, sharedStyles, spacing } from '@/ui/theme'

export default function SignupScreen() {
    const { register } = useAuth()
    const router = useRouter()

    const [displayName, setDisplayName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState(false)

    const handleSignup = async () => {
        setError(null)
        if (password !== confirmPassword) {
            setError('Passwords do not match.')
            return
        }
        if (password.length < 6) {
            setError('Password must be at least 6 characters.')
            return
        }
        setLoading(true)
        try {
            await register(email.trim(), password, displayName.trim())
            router.replace('/(app)/clipboard')
        } catch (err) {
            setError(err instanceof AuthException ? err.message : 'Registration failed.')
        } finally {
            setLoading(false)
        }
    }

    return (
        <KeyboardAvoidingView
            style={sharedStyles.screen}
            behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        >
            <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
                <Text style={sharedStyles.title}>Create your account</Text>
                <Text style={[sharedStyles.subtitle, styles.gapBottom]}>
                    Start syncing your clipboard everywhere.
                </Text>

                {error && <Text style={[sharedStyles.errorText, styles.gapBottom]}>{error}</Text>}

                <View style={styles.field}>
                    <Text style={sharedStyles.label}>Display name</Text>
                    <TextInput
                        style={sharedStyles.input}
                        placeholder="Your name"
                        placeholderTextColor={colors.textMuted}
                        value={displayName}
                        onChangeText={setDisplayName}
                    />
                </View>

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

                <View style={styles.field}>
                    <Text style={sharedStyles.label}>Confirm password</Text>
                    <TextInput
                        style={sharedStyles.input}
                        placeholder="••••••••"
                        placeholderTextColor={colors.textMuted}
                        secureTextEntry
                        value={confirmPassword}
                        onChangeText={setConfirmPassword}
                    />
                </View>

                <Button title="Sign up" onPress={handleSignup} loading={loading} />
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
    gapBottom: {
        marginBottom: spacing.sm,
    },
    field: {
        gap: spacing.xs,
    },
})
