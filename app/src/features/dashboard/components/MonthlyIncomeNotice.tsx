import { useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import type { DashboardData, OnboardingStatus } from '../../../types/api'
import { EntryActionButton } from './EntryActionButton'

type MonthlyIncomeNoticeProps = {
  dashboard: DashboardData
  reminderStatus: OnboardingStatus | null
  isFutureMonth: boolean
  onRegisterIncome(): void
  onReminderUpdated(): void
}

export function MonthlyIncomeNotice({ dashboard, reminderStatus, isFutureMonth, onRegisterIncome, onReminderUpdated }: MonthlyIncomeNoticeProps) {
  const [showSettings, setShowSettings] = useState(false)
  const [reminderDay, setReminderDay] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function saveReminder(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await apiRequest<OnboardingStatus>('/onboarding/income-reminder', {
        method: 'PUT',
        body: JSON.stringify({ dayOfMonth: Number(reminderDay) }),
      })
      setShowSettings(false)
      onReminderUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível configurar o lembrete.')
    } finally { setSaving(false) }
  }

  function toggleSettings() {
    setReminderDay(reminderStatus?.monthlyIncomeReminderDay?.toString() ?? '')
    setError('')
    setShowSettings((current) => !current)
  }

  return (
    <section className={`income-notice${dashboard.incomeReminderDue ? ' income-notice-due' : ''}`} aria-label="Recebimento do mês">
      <div className="income-notice-main">
        <span className="income-notice-dot" aria-hidden="true" />
        <div>
          <strong>{dashboard.monthlyIncomeRecorded ? 'Recebimento registrado' : dashboard.incomeReminderDue ? 'Recebimento pendente' : 'Recebimento do mês'}</strong>
          <p>{dashboard.monthlyIncomeRecorded
            ? 'O aviso deste mês não aparecerá novamente.'
            : isFutureMonth
              ? 'Você poderá registrar o valor quando receber.'
              : dashboard.incomeReminderDue
                ? 'O dia do lembrete chegou. Informe o valor recebido quando puder.'
                : 'Você pode registrar o valor assim que receber.'}</p>
        </div>
      </div>
      <div className="income-notice-actions">
        {!dashboard.monthlyIncomeRecorded && !isFutureMonth && <EntryActionButton type="Income" label="Registrar recebimento" onClick={onRegisterIncome} />}
        <button className="income-notice-manage" type="button" aria-controls="income-reminder-form" aria-expanded={showSettings} onClick={toggleSettings}>{reminderStatus?.incomeSetupMode === 'MonthlyReminder' && reminderStatus.monthlyIncomeReminderDay ? `Aviso: dia ${reminderStatus.monthlyIncomeReminderDay}` : 'Configurar aviso'}<svg aria-hidden="true" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><path d="m4 6 4 4 4-4" /></svg></button>
      </div>
      {showSettings && <form id="income-reminder-form" className="income-notice-form" onSubmit={(event) => { void saveReminder(event) }}><label>Dia do mês para receber o lembrete<input type="number" inputMode="numeric" min="1" max="28" step="1" value={reminderDay} onChange={(event) => setReminderDay(event.target.value)} required /><span className="field-hint">Escolha de 1 a 28 para funcionar em todos os meses.</span></label><button className="button button-primary" type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Salvar dia'}</button></form>}
      {error && <div className="form-error" role="alert">{error}</div>}
    </section>
  )
}
