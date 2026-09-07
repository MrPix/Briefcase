import { useEffect } from 'react'
import { ActivityIndicator, Pressable, Text, View, type ColorValue } from 'react-native'
import { Redirect, Tabs, useRouter } from 'expo-router'
import { useAuth } from '@/auth/AuthContext'
import { colors, sharedStyles } from '@/ui/theme'

export default function AppLayout() {
    const { isAuthenticated, restoring, logout } = useAuth()
    const router = useRouter()

    useEffect(() => {
        if (!restoring && !isAuthenticated) router.replace('/login')
    }, [restoring, isAuthenticated, router])

    if (restoring) {
        return (
            <View style={[sharedStyles.screen, { alignItems: 'center', justifyContent: 'center' }]}>
                <ActivityIndicator color={colors.accent} size="large" />
            </View>
        )
    }

    if (!isAuthenticated) return <Redirect href="/login" />

    return (
        <Tabs
            screenOptions={{
                headerStyle: { backgroundColor: colors.bg },
                headerTintColor: colors.text,
                headerShadowVisible: false,
                sceneStyle: { backgroundColor: colors.bg },
                tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border },
                tabBarActiveTintColor: colors.accentSoft,
                tabBarInactiveTintColor: colors.textMuted,
                headerRight: () => (
                    <Pressable onPress={() => logout()} hitSlop={12} style={{ marginRight: 16 }}>
                        <Text style={{ color: colors.accentSoft, fontWeight: '600' }}>Sign out</Text>
                    </Pressable>
                ),
            }}
        >
            <Tabs.Screen
                name="clipboard"
                options={{
                    title: 'Clipboard',
                    tabBarIcon: ({ color }) => <TabDot color={color} />,
                }}
            />
            <Tabs.Screen
                name="files"
                options={{
                    title: 'Files',
                    tabBarIcon: ({ color }) => <TabDot color={color} />,
                }}
            />
        </Tabs>
    )
}

function TabDot({ color }: { color: ColorValue }) {
    return <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: color }} />
}
