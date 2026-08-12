import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import en from './locales/en.json'
import uk from './locales/uk.json'

export const SUPPORTED_LANGUAGES = ['en', 'uk'] as const
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number]

// Persistence is handled by LanguageContext (localStorage + server), not the detector's own cache.
i18n
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
        resources: { en: { translation: en }, uk: { translation: uk } },
        fallbackLng: 'en',
        supportedLngs: [...SUPPORTED_LANGUAGES],
        nonExplicitSupportedLngs: true,
        load: 'languageOnly',
        detection: { order: ['navigator'], caches: [] },
        interpolation: { escapeValue: false },
    })

export default i18n
