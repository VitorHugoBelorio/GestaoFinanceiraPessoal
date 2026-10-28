import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { AuthShell } from '../components/layout/AuthShell'

export function RegisterPage() {
  const { user, registrationDraft, setRegistrationDraft } = useAuth()
  const navigate = useNavigate()
  const [name, setName] = useState(registrationDraft?.name ?? '')
  const [email, setEmail] = useState(registrationDraft?.email ?? '')
  const [password, setPassword] = useState(registrationDraft?.password ?? '')
  const [confirmPassword, setConfirmPassword] = useState(registrationDraft?.password ?? '')
  const [error, setError] = useState('')

  if (user) return <Navigate to={user.onboardingCompleted ? '/app' : '/primeiros-passos'} replace />

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (password !== confirmPassword) { setError('As senhas informadas não são iguais.'); return }
    setRegistrationDraft({ name, email, password })
    navigate('/primeiros-passos')
  }

  return (
    <AuthShell mode="register">
      <section className="auth-card">
        <Link className="back-link" to="/">← Voltar</Link>
        <span className="eyebrow">Cadastro</span>
        <h1>Crie sua conta</h1>
        <p>Depois disso, vamos configurar seu ponto de partida financeiro. A conta só será criada ao concluir todas as etapas; se atualizar a página antes, será preciso recomeçar.</p>
        <form onSubmit={handleSubmit}>
          <label>Nome<input autoComplete="name" value={name} onChange={(event) => setName(event.target.value)} minLength={2} maxLength={120} required /></label>
          <label>E-mail<input type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={256} required /></label>
          <label>Senha<input type="password" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} minLength={8} maxLength={128} required /></label>
          <label>Confirmar senha<input type="password" autoComplete="new-password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} minLength={8} maxLength={128} required /></label>
          {error && <div className="form-error">{error}</div>}
          <button className="button auth-submit" type="submit">
            Continuar cadastro
            <span aria-hidden="true">→</span>
          </button>
        </form>
        <p className="auth-footer">Já possui uma conta? <Link to="/acesso">Entrar</Link></p>
      </section>
    </AuthShell>
  )
}
