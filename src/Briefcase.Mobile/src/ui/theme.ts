import { StyleSheet } from 'react-native'

// Design tokens ported from the Briefcase web app (accent #0EA5E9 / #38BDF8).
export const colors = {
    bg: '#0F172A',
    surface: '#1E293B',
    surfaceAlt: '#334155',
    border: '#334155',
    text: '#F1F5F9',
    textMuted: '#94A3B8',
    accent: '#0EA5E9',
    accentSoft: '#38BDF8',
    danger: '#EF4444',
    success: '#22C55E',
    onAccent: '#0B1120',
}

export const spacing = {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24,
    xl: 32,
}

export const radius = {
    sm: 8,
    md: 12,
    lg: 16,
    pill: 999,
}

export const sharedStyles = StyleSheet.create({
    screen: {
        flex: 1,
        backgroundColor: colors.bg,
    },
    input: {
        backgroundColor: colors.surface,
        borderColor: colors.border,
        borderWidth: 1,
        borderRadius: radius.md,
        color: colors.text,
        paddingHorizontal: spacing.md,
        paddingVertical: spacing.sm + 4,
        fontSize: 16,
    },
    label: {
        color: colors.textMuted,
        fontSize: 13,
        marginBottom: spacing.xs,
        fontWeight: '600',
    },
    title: {
        color: colors.text,
        fontSize: 26,
        fontWeight: '700',
    },
    subtitle: {
        color: colors.textMuted,
        fontSize: 15,
    },
    errorText: {
        color: colors.danger,
        fontSize: 14,
    },
})
