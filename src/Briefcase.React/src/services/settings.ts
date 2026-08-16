import { api } from '../lib/apiClient'

export interface UserSettings {
    language: string | null
    googleMapsNavigationEnabled: boolean
    navigationApplicationIds: string[]
    navigationApplications: NavigationApplication[]
}

export interface NavigationApplication {
    id: string
    displayName: string
}

export const settingsApi = {
    get(): Promise<UserSettings> {
        return api.get<UserSettings>('api/users/settings')
    },

    update(language: string | null): Promise<void> {
        return api.put<void>('api/users/settings', { language })
    },

    updateNavigation(enabled: boolean, applicationIds: string[]): Promise<UserSettings> {
        return api.put<UserSettings>('api/users/settings/navigation', { enabled, applicationIds })
    },
}
