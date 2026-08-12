import { useTranslation } from 'react-i18next'
import { useLanguage } from '../i18n/LanguageContext'
import type { SupportedLanguage } from '../i18n/config'

const LANGUAGE_LABEL_KEYS: Record<SupportedLanguage, string> = {
    en: 'languageSwitcher.english',
    uk: 'languageSwitcher.ukrainian',
}

export interface LanguageSwitcherProps {
    onChange?: (lang: SupportedLanguage) => void
    onError?: (error: unknown) => void
}

export function LanguageSwitcher({ onChange, onError }: LanguageSwitcherProps) {
    const { t } = useTranslation()
    const { language, supportedLanguages, setLanguage } = useLanguage()

    const handleChange = async (lang: SupportedLanguage) => {
        try {
            await setLanguage(lang)
            onChange?.(lang)
        } catch (err) {
            onError?.(err)
        }
    }

    return (
        <select
            className="form-control language-switcher"
            aria-label={t('languageSwitcher.label')}
            value={language}
            onChange={(e) => handleChange(e.target.value as SupportedLanguage)}
        >
            {supportedLanguages.map((lang) => (
                <option key={lang} value={lang}>
                    {t(LANGUAGE_LABEL_KEYS[lang])}
                </option>
            ))}
        </select>
    )
}
