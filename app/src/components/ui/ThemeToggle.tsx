import { useTheme } from '../../theme/ThemeContext'

export function ThemeToggle({ compact = false }: { compact?: boolean }) {
  const { theme, toggleTheme } = useTheme()
  const nextTheme = theme === 'light' ? 'escuro' : 'claro'

  return (
    <button
      aria-label={`Ativar tema ${nextTheme}`}
      className={`theme-toggle${compact ? ' theme-toggle-compact' : ''}`}
      onClick={toggleTheme}
      title={`Ativar tema ${nextTheme}`}
      type="button"
    >
      {theme === 'light' ? (
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20.2 15.4A8.5 8.5 0 0 1 8.6 3.8 8.5 8.5 0 1 0 20.2 15.4Z"/></svg>
      ) : (
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3v2m0 14v2M3 12h2m14 0h2M5.64 5.64l1.41 1.41m9.9 9.9 1.41 1.41m0-12.72-1.41 1.41m-9.9 9.9-1.41 1.41"/><circle cx="12" cy="12" r="4"/></svg>
      )}
      {!compact && <span>Tema {nextTheme}</span>}
    </button>
  )
}
