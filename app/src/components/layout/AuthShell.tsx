import type { ReactNode } from 'react'
import { Brand } from '../ui/Brand'
import { ThemeToggle } from '../ui/ThemeToggle'

export function AuthShell({ mode, children }: { mode: 'login' | 'register'; children: ReactNode }) {
  const isLogin = mode === 'login'

  return (
    <main className="auth-page">
      <header className="auth-topbar"><Brand /><ThemeToggle compact /></header>
      <div className="auth-layout">
        <aside className="auth-context" aria-label="Sobre o controle financeiro">
          <span className="eyebrow">Organização com leveza</span>
          <h2>{isLogin ? 'Seu panorama financeiro continua aqui.' : 'Comece com uma visão clara do seu dinheiro.'}</h2>
          <p>{isLogin
            ? 'Acesse para registrar seus movimentos e acompanhar a evolução dos seus saldos.'
            : 'Configure seu ponto de partida e adapte a rotina financeira ao seu jeito de trabalhar.'}</p>
          <div className="auth-context-visual" aria-hidden="true">
            <span /><span /><span /><span /><span />
          </div>
        </aside>
        {children}
      </div>
    </main>
  )
}
