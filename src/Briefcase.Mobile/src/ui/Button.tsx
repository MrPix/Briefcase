import { ActivityIndicator, Pressable, StyleSheet, Text, type PressableProps } from 'react-native'
import { colors, radius, spacing } from './theme'

interface ButtonProps extends Omit<PressableProps, 'style'> {
    title: string
    variant?: 'primary' | 'outline' | 'danger'
    loading?: boolean
}

export function Button({ title, variant = 'primary', loading, disabled, ...rest }: ButtonProps) {
    const isDisabled = disabled || loading
    return (
        <Pressable
            accessibilityRole="button"
            disabled={isDisabled}
            style={({ pressed }) => [
                styles.base,
                variant === 'primary' && styles.primary,
                variant === 'outline' && styles.outline,
                variant === 'danger' && styles.danger,
                (pressed || isDisabled) && styles.dim,
            ]}
            {...rest}
        >
            {loading ? (
                <ActivityIndicator color={variant === 'primary' ? colors.onAccent : colors.text} />
            ) : (
                <Text
                    style={[
                        styles.label,
                        variant === 'primary' ? styles.labelPrimary : styles.labelOther,
                    ]}
                >
                    {title}
                </Text>
            )}
        </Pressable>
    )
}

const styles = StyleSheet.create({
    base: {
        borderRadius: radius.md,
        paddingVertical: spacing.sm + 4,
        paddingHorizontal: spacing.md,
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: 48,
    },
    primary: {
        backgroundColor: colors.accent,
    },
    outline: {
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: 'transparent',
    },
    danger: {
        backgroundColor: colors.danger,
    },
    dim: {
        opacity: 0.6,
    },
    label: {
        fontSize: 16,
        fontWeight: '600',
    },
    labelPrimary: {
        color: colors.onAccent,
    },
    labelOther: {
        color: colors.text,
    },
})
