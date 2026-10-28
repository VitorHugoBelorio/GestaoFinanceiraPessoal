import { useEffect, useState, type FormEvent } from 'react'
import { AnimatePresence } from 'motion/react'
import * as m from 'motion/react-m'
import { Navigate, useNavigate } from 'react-router-dom'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Brand } from '../components/ui/Brand'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { ThemeToggle } from '../components/ui/ThemeToggle'
import { panelVariants } from '../motion/variants'
import type { EmploymentType, IncomeSetupMode } from '../types/api'
import { parseMoney } from '../utils/money'

export function OnboardingPage() {
  const { user, refresh, registrationDraft, registrationSetupDraft, setRegistrationSetupDraft, completeRegistration } = useAuth()
  const navigate = useNavigate()
  const [step, setStep] = useState(1)
  const [accountName, setAccountName] = useState(user?.name ?? '')
  const [accountEmail, setAccountEmail] = useState(user?.email ?? '')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmNewPassword, setConfirmNewPassword] = useState('')
  const [savingAccount, setSavingAccount] = useState(false)
  const [initialCapital, setInitialCapital] = useState(registrationSetupDraft?.initialCapital ?? '')
  const [employmentType, setEmploymentType] = useState<EmploymentType>(registrationSetupDraft?.employmentType ?? 'Clt')
  const [fixedIncomeEnabled, setFixedIncomeEnabled] = useState(registrationSetupDraft?.fixedIncomeEnabled ?? false)
  const [reminderEnabled, setReminderEnabled] = useState(registrationSetupDraft?.reminderEnabled ?? false)
  const [fixedMonthlyIncome, setFixedMonthlyIncome] = useState(registrationSetupDraft?.fixedMonthlyIncome ?? '')
  const [incomeDayOfMonth, setIncomeDayOfMonth] = useState(registrationSetupDraft?.incomeDayOfMonth ?? '')
  const [reminderDay, setReminderDay] = useState(registrationSetupDraft?.reminderDay ?? '')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!registrationDraft) return
    setRegistrationSetupDraft({ initialCapital, employmentType, fixedIncomeEnabled, reminderEnabled, fixedMonthlyIncome, incomeDayOfMonth, reminderDay })
  }, [registrationDraft, setRegistrationSetupDraft, initialCapital, employmentType, fixedIncomeEnabled, reminderEnabled, fixedMonthlyIncome, incomeDayOfMonth, reminderDay])

  if (user?.onboardingCompleted) return <Navigate to="/app" replace />

  async function handleAccountSave(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (newPassword !== confirmNewPassword) {
      setError('As novas senhas informadas não são iguais.')
      return
    }

    setSavingAccount(true)
    try {
      await apiRequest('/auth/account', {
        method: 'PUT',
        body: JSON.stringify({
          name: accountName,
          email: accountEmail,
          currentPassword,
          newPassword: newPassword || null,
        }),
      })
      await refresh()
      setCurrentPassword('')
      setNewPassword('')
      setConfirmNewPassword('')
      setStep(1)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível atualizar seus dados.')
    } finally { setSavingAccount(false) }
  }

  function cancelAccountReview() {
    setAccountName(user?.name ?? '')
    setAccountEmail(user?.email ?? '')
    setCurrentPassword('')
    setNewPassword('')
    setConfirmNewPassword('')
    setError('')
    setStep(1)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError('')
    const incomeSetupMode: IncomeSetupMode = employmentType === 'Clt'
      ? (fixedIncomeEnabled ? 'FixedMonthly' : 'Manual')
      : (reminderEnabled ? 'MonthlyReminder' : 'Manual')
    if (incomeSetupMode === 'FixedMonthly' && (!fixedMonthlyIncome || parseMoney(fixedMonthlyIncome) <= 0)) {
      setError('Informe uma renda mensal maior que zero.')
      return
    }
    setSubmitting(true)
    try {
      const setup = {
        initialCapital: parseMoney(initialCapital),
        employmentType,
        incomeSetupMode,
        fixedMonthlyIncome: incomeSetupMode === 'FixedMonthly' ? parseMoney(fixedMonthlyIncome) : null,
        incomeDayOfMonth: incomeSetupMode === 'FixedMonthly' ? Number(incomeDayOfMonth) : null,
        incomeReminderDay: incomeSetupMode === 'MonthlyReminder' ? Number(reminderDay) : null,
      }
      if (registrationDraft) {
        await completeRegistration(setup)
      } else {
        await apiRequest('/onboarding/complete', { method: 'POST', body: JSON.stringify(setup) })
        await refresh()
      }
      navigate('/app')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível concluir a configuração. Tente novamente.')
    } finally { setSubmitting(false) }
  }

  return (
    <main className="onboarding-page">
      <header className="onboarding-topbar"><Brand /><ThemeToggle compact /></header>
      <section className="onboarding-card">
        {step > 0 && <div className="step-indicator"><span>Etapa {step} de 2</span><div aria-hidden="true"><i className={step >= 1 ? 'complete' : ''} /><i className={step >= 2 ? 'complete' : ''} /></div></div>}
        <AnimatePresence mode="wait" initial={false}>
          {step === 0 ? (
          <m.form key="account" variants={panelVariants} initial="hidden" animate="visible" exit="exit" onSubmit={handleAccountSave}>
            <span className="eyebrow">Revisar cadastro</span>
            <h1>Seus dados de acesso</h1>
            <p>Corrija seus dados antes de continuar. O capital informado ficará guardado nesta etapa.</p>
            <label>Nome<input autoComplete="name" value={accountName} onChange={(event) => setAccountName(event.target.value)} minLength={2} maxLength={120} required /></label>
            <label>E-mail<input type="email" autoComplete="email" value={accountEmail} onChange={(event) => setAccountEmail(event.target.value)} maxLength={256} required /></label>
            <label>Senha atual<input type="password" autoComplete="current-password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} required /><span className="field-hint">Confirme sua senha para salvar alterações na conta.</span></label>
            <label>Nova senha (opcional)<input type="password" autoComplete="new-password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} minLength={8} maxLength={128} /></label>
            {newPassword && <label>Confirmar nova senha<input type="password" autoComplete="new-password" value={confirmNewPassword} onChange={(event) => setConfirmNewPassword(event.target.value)} minLength={8} maxLength={128} required /></label>}
            {error && <div className="form-error" role="alert">{error}</div>}
            <div className="form-actions"><button className="button button-ghost" type="button" onClick={cancelAccountReview}>Cancelar</button><button aria-busy={savingAccount} className="button auth-submit" disabled={savingAccount} type="submit">{savingAccount ? 'Salvando...' : 'Salvar e voltar'}{!savingAccount && <span aria-hidden="true">→</span>}</button></div>
          </m.form>
        ) : step === 1 ? (
          <m.div key="capital" className="onboarding-step" variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <button className="back-link" type="button" onClick={() => { setError(''); if (registrationDraft) navigate('/cadastro'); else setStep(0) }}>← Voltar ao cadastro</button>
            <span className="eyebrow">Seu ponto de partida</span>
            <h1>Qual é o seu capital total atual?</h1>
            <p>Considere o valor disponível hoje em contas, carteira e reservas que deseja acompanhar.</p>
            <label>Capital atual<CurrencyInput value={initialCapital} onValueChange={setInitialCapital} /><span className="field-hint">Digite 0 se estiver começando sem saldo.</span></label>
            <button className="button auth-submit" disabled={initialCapital === '' || !Number.isFinite(parseMoney(initialCapital)) || parseMoney(initialCapital) < 0} onClick={() => { setError(''); setStep(2) }}>Continuar <span aria-hidden="true">→</span></button>
          </m.div>
        ) : (
          <m.form key="profile" variants={panelVariants} initial="hidden" animate="visible" exit="exit" onSubmit={handleSubmit}>
            <span className="eyebrow">Personalização inicial</span>
            <h1>Como sua renda funciona hoje?</h1>
            <p>Essas escolhas não limitam o sistema. Você poderá alterá-las e usar qualquer forma de lançamento.</p>
            <fieldset>
              <legend>Perfil profissional</legend>
              <div className="option-grid two-columns">
                <button className={employmentType === 'Clt' ? 'option selected' : 'option'} type="button" onClick={() => { setError(''); setEmploymentType('Clt') }}><strong>CLT</strong><span>Renda normalmente previsível</span></button>
                <button className={employmentType === 'Pj' ? 'option selected' : 'option'} type="button" onClick={() => { setError(''); setEmploymentType('Pj') }}><strong>PJ</strong><span>Renda pode variar por mês</span></button>
              </div>
            </fieldset>
            <fieldset>
              <legend>Registro da renda</legend>
              <p className="income-default">Por padrão, você registra sua renda manualmente quando receber.</p>
              {employmentType === 'Clt' ? (
                <label className="income-choice"><input type="checkbox" checked={fixedIncomeEnabled} onChange={(event) => { setError(''); setFixedIncomeEnabled(event.target.checked) }} /><span><strong>Registrar salário fixo automaticamente</strong><small>O sistema cria a receita mensal a partir do próximo mês.</small></span></label>
              ) : (
                <label className="income-choice"><input type="checkbox" checked={reminderEnabled} onChange={(event) => { setError(''); setReminderEnabled(event.target.checked) }} /><span><strong>Receber lembrete mensal</strong><small>Você informa o valor recebido no mês.</small></span></label>
              )}
            </fieldset>
            {employmentType === 'Clt' && fixedIncomeEnabled && <><label>Valor mensal<CurrencyInput value={fixedMonthlyIncome} onValueChange={(value) => { setError(''); setFixedMonthlyIncome(value) }} /></label><label>Dia do recebimento (1 a 31)<input type="number" min="1" max="31" value={incomeDayOfMonth} onChange={(event) => setIncomeDayOfMonth(event.target.value)} required /></label></>}
            {employmentType === 'Pj' && reminderEnabled && <label>Dia do mês para receber o lembrete<input type="number" inputMode="numeric" min="1" max="28" step="1" value={reminderDay} onChange={(event) => setReminderDay(event.target.value)} required /><span className="field-hint">Escolha de 1 a 28, para que o lembrete exista em todos os meses.</span></label>}
            {error && <div className="form-error" role="alert">{error}</div>}
            <div className="form-actions"><button className="button button-ghost" type="button" onClick={() => { setError(''); setStep(1) }}>Voltar</button><button aria-busy={submitting} className="button auth-submit" disabled={submitting} type="submit">{submitting ? 'Configurando...' : 'Concluir configuração'}{!submitting && <span aria-hidden="true">→</span>}</button></div>
          </m.form>
        )}
        </AnimatePresence>
      </section>
    </main>
  )
}
