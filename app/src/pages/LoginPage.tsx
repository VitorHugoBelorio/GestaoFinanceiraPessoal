import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AuthShell } from '../components/layout/AuthShell'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to={user.onboardingCompleted ? '/app' : '/primeiros-passos'} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const session = await login({ email, password })
      navigate(session.onboardingCompleted ? '/app' : '/primeiros-passos')
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível entrar.')
    } finally { setSubmitting(false) }
  }

  return (
    <AuthShell mode="login">
      <section className="auth-card">
        <Link className="back-link" to="/">← Voltar</Link>
        <span className="eyebrow">Acesso</span>
        <h1>Bem-vindo de volta</h1>
        <p>Entre para acessar seu panorama financeiro.</p>
        <form onSubmit={handleSubmit}>
          <label>E-mail<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required /></label>
          <label>Senha<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required /></label>
          {error && <div className="form-error">{error}</div>}
          <button aria-busy={submitting} className="button auth-submit" disabled={submitting} type="submit">
            {submitting ? 'Entrando...' : 'Entrar na minha conta'}
            {!submitting && <span aria-hidden="true">→</span>}
          </button>
        </form>
        <p className="auth-footer">Ainda não tem uma conta? <Link to="/cadastro">Cadastre-se</Link></p>
      </section>
    </AuthShell>
  )
}
