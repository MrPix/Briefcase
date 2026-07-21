import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState,
    type ReactNode,
} from 'react'

export type ThemePreference = 'system' | 'light' | 'dark'

const STORAGE_KEY = 'briefcase_theme'

interface ThemeContextValue {
    preference: ThemePreference
    resolved: 'light' | 'dark'
    setPreference: (pref: ThemePreference) => void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

function readStored(): ThemePreference {
    const v = localStorage.getItem(STORAGE_KEY)
    return v === 'light' || v === 'dark' || v === 'system' ? v : 'system'
}

function systemPrefersDark(): boolean {
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false
}

export function ThemeProvider({ children }: { children: ReactNode }) {
    const [preference, setPreferenceState] = useState<ThemePreference>(readStored)
    const [systemDark, setSystemDark] = useState(systemPrefersDark)

    useEffect(() => {
        const mq = window.matchMedia('(prefers-color-scheme: dark)')
        const handler = (e: MediaQueryListEvent) => setSystemDark(e.matches)
        mq.addEventListener('change', handler)
        return () => mq.removeEventListener('change', handler)
    }, [])

    const resolved: 'light' | 'dark' =
        preference === 'system' ? (systemDark ? 'dark' : 'light') : preference

    useEffect(() => {
        document.documentElement.setAttribute('data-theme', resolved)
    }, [resolved])

    const setPreference = useCallback((pref: ThemePreference) => {
        localStorage.setItem(STORAGE_KEY, pref)
        setPreferenceState(pref)
    }, [])

    const value = useMemo<ThemeContextValue>(
        () => ({ preference, resolved, setPreference }),
        [preference, resolved, setPreference],
    )

    return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme(): ThemeContextValue {
    const ctx = useContext(ThemeContext)
    if (!ctx) throw new Error('useTheme must be used within a ThemeProvider.')
    return ctx
}
