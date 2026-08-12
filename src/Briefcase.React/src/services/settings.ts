import { api } from '../lib/apiClient'

export interface UserSettings {
    language: string | null
}

export const settingsApi = {
    get(): Promise<UserSettings> {
        return api.get<UserSettings>('api/users/settings')
    },

    update(language: string | null): Promise<void> {
        return api.put<void>('api/users/settings', { language })
    },
}
