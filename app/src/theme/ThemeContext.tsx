import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

type Theme = 'light' | 'dark'

interface ThemeContextValue {
  theme: Theme
  toggleTheme(): void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)
const storageKey = 'gestao-financeira-theme'

function getInitialTheme(): Theme {
  try {
    const storedTheme = window.localStorage.getItem(storageKey)
    if (storedTheme === 'light' || storedTheme === 'dark') return storedTheme
  } catch {
    // Alguns navegadores podem bloquear o armazenamento local em modos restritos.
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(getInitialTheme)

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    document.documentElement.style.colorScheme = theme
    try {
      window.localStorage.setItem(storageKey, theme)
    } catch {
      // O tema continua funcionando durante a sessão, mesmo sem persistência.
    }
  }, [theme])

  const value = useMemo(() => ({
    theme,
    toggleTheme: () => setTheme((current) => current === 'light' ? 'dark' : 'light'),
  }), [theme])

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const context = useContext(ThemeContext)
  if (!context) throw new Error('useTheme deve ser usado dentro de ThemeProvider.')
  return context
}
