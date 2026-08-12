import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState,
    type ReactNode,
} from 'react'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'
import { settingsApi } from '../services/settings'
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from './config'

const STORAGE_KEY = 'briefcase_language'

interface LanguageContextValue {
    language: SupportedLanguage
    supportedLanguages: readonly SupportedLanguage[]
    setLanguage: (lang: SupportedLanguage) => Promise<void>
}

const LanguageContext = createContext<LanguageContextValue | null>(null)

function isSupported(lang: string): lang is SupportedLanguage {
    return (SUPPORTED_LANGUAGES as readonly string[]).includes(lang)
}

export function LanguageProvider({ children }: { children: ReactNode }) {
    const { i18n } = useTranslation()
    const { isAuthenticated } = useAuth()
    const [language, setLanguageState] = useState<SupportedLanguage>(
        isSupported(i18n.language) ? i18n.language : 'en',
    )

    useEffect(() => {
        const stored = localStorage.getItem(STORAGE_KEY)
        if (stored && isSupported(stored) && stored !== i18n.language) {
            i18n.changeLanguage(stored)
            setLanguageState(stored)
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    // Server preference wins for signed-in users, so the choice follows the account across devices.
    useEffect(() => {
        if (!isAuthenticated) return
        let cancelled = false
            ; (async () => {
                try {
                    const settings = await settingsApi.get()
                    if (cancelled || !settings.language || !isSupported(settings.language)) return
                    if (settings.language !== i18n.language) {
                        await i18n.changeLanguage(settings.language)
                        localStorage.setItem(STORAGE_KEY, settings.language)
                    }
                    setLanguageState(settings.language)
                } catch {
                    /* keep current language if the server is unreachable */
                }
            })()
        return () => {
            cancelled = true
        }
    }, [isAuthenticated, i18n])

    useEffect(() => {
        document.documentElement.lang = language
    }, [language])

    const setLanguage = useCallback(
        async (lang: SupportedLanguage) => {
            await i18n.changeLanguage(lang)
            localStorage.setItem(STORAGE_KEY, lang)
            setLanguageState(lang)
            if (isAuthenticated) {
                await settingsApi.update(lang)
            }
        },
        [i18n, isAuthenticated],
    )

    const value = useMemo<LanguageContextValue>(
        () => ({ language, supportedLanguages: SUPPORTED_LANGUAGES, setLanguage }),
        [language, setLanguage],
    )

    return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage(): LanguageContextValue {
    const ctx = useContext(LanguageContext)
    if (!ctx) throw new Error('useLanguage must be used within a LanguageProvider.')
    return ctx
}
