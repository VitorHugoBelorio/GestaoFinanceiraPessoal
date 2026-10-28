import { Link } from 'react-router-dom'
import { Brand } from '../ui/Brand'
import { ThemeToggle } from '../ui/ThemeToggle'

export function PublicHeader() {
  return (
    <header className="public-header">
      <Brand />
      <nav aria-label="Navegação principal">
        <ThemeToggle compact />
        <Link className="public-nav-link" to="/acesso">Entrar</Link>
        <Link className="button button-primary button-nav" to="/cadastro">Criar conta <span aria-hidden="true">↗</span></Link>
      </nav>
    </header>
  )
}
