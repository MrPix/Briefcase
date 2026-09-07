import { Redirect } from 'expo-router'
import { ActivityIndicator, View } from 'react-native'
import { useAuth } from '@/auth/AuthContext'
import { colors, sharedStyles } from '@/ui/theme'

export default function Index() {
    const { isAuthenticated, restoring } = useAuth()

    if (restoring) {
        return (
            <View style={[sharedStyles.screen, { alignItems: 'center', justifyContent: 'center' }]}>
                <ActivityIndicator color={colors.accent} size="large" />
            </View>
        )
    }

    return <Redirect href={isAuthenticated ? '/(app)/clipboard' : '/login'} />
}
