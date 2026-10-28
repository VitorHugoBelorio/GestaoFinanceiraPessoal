import { useEffect, useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import { CurrencyInput } from '../../../components/ui/CurrencyInput'
import type { DashboardData, InvestmentMovementType } from '../../../types/api'
import { parseMoney } from '../../../utils/money'
import { currencyFormatter } from '../dashboard.formatters'

type CapitalPlanPanelProps = {
  dashboard: DashboardData
  onUpdated(): void
}

type ActiveForm = 'opening' | 'budget' | 'movement' | null

const inputMoneyFormatter = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

function defaultMovementDate(year: number, month: number): string {
  const today = new Date()
  const lastDay = new Date(year, month, 0)
  const selected = lastDay < today ? lastDay : today
  return `${selected.getFullYear()}-${String(selected.getMonth() + 1).padStart(2, '0')}-${String(selected.getDate()).padStart(2, '0')}`
}

export function CapitalPlanPanel({ dashboard, onUpdated }: CapitalPlanPanelProps) {
  const [activeForm, setActiveForm] = useState<ActiveForm>(null)
  const [openingAmount, setOpeningAmount] = useState(inputMoneyFormatter.format(dashboard.openingInvestedAmount))
  const [budgetAmount, setBudgetAmount] = useState(dashboard.monthlySpendingPlan === null ? '' : inputMoneyFormatter.format(dashboard.monthlySpendingPlan))
  const [movementType, setMovementType] = useState<InvestmentMovementType>('Contribution')
  const [movementAmount, setMovementAmount] = useState('')
  const [movementDate, setMovementDate] = useState(defaultMovementDate(dashboard.year, dashboard.month))
  const [movementNote, setMovementNote] = useState('')
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const today = new Date()
  const isFutureMonth = new Date(dashboard.year, dashboard.month - 1, 1) > new Date(today.getFullYear(), today.getMonth(), 1)
  const maxMovementDate = defaultMovementDate(dashboard.year, dashboard.month)
  const minMovementDate = `${dashboard.year}-${String(dashboard.month).padStart(2, '0')}-01`

  useEffect(() => {
    if (activeForm !== null) return
    setOpeningAmount(inputMoneyFormatter.format(dashboard.openingInvestedAmount))
    setBudgetAmount(dashboard.monthlySpendingPlan === null ? '' : inputMoneyFormatter.format(dashboard.monthlySpendingPlan))
  }, [activeForm, dashboard.openingInvestedAmount, dashboard.monthlySpendingPlan])

  function openForm(form: ActiveForm) {
    setError('')
    setConfirmDeleteId(null)
    setActiveForm((current) => current === form ? null : form)
  }

  async function saveOpening(event: FormEvent) {
    event.preventDefault()
    const amount = parseMoney(openingAmount)
    if (!Number.isFinite(amount) || amount < 0 || amount > dashboard.initialCapital) {
      setError(`Informe um valor entre R$ 0,00 e ${currencyFormatter.format(dashboard.initialCapital)}.`)
      return
    }

    setBusy(true)
    setError('')
    try {
      await apiRequest<void>('/investments/opening-amount', {
        method: 'PUT', body: JSON.stringify({ amount }),
      })
      setActiveForm(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível salvar o valor investido.')
    } finally { setBusy(false) }
  }

  async function saveBudget(event: FormEvent) {
    event.preventDefault()
    const amount = parseMoney(budgetAmount)
    if (!Number.isFinite(amount) || amount < 0) {
      setError('Informe um limite mensal válido.')
      return
    }

    setBusy(true)
    setError('')
    try {
      await apiRequest<void>('/investments/monthly-spending-plan', {
        method: 'PUT', body: JSON.stringify({ year: dashboard.year, month: dashboard.month, amount }),
      })
      setActiveForm(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível salvar o planejamento.')
    } finally { setBusy(false) }
  }

  async function removeBudget() {
    setBusy(true)
    setError('')
    try {
      await apiRequest<void>('/investments/monthly-spending-plan', {
        method: 'PUT', body: JSON.stringify({ year: dashboard.year, month: dashboard.month, amount: null }),
      })
      setActiveForm(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível remover o planejamento.')
    } finally { setBusy(false) }
  }

  async function saveMovement(event: FormEvent) {
    event.preventDefault()
    const amount = parseMoney(movementAmount)
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Informe um valor maior que zero.')
      return
    }
    if (movementType === 'Withdrawal' && amount > dashboard.investedBalance) {
      setError('O resgate não pode superar o valor aplicado registrado.')
      return
    }
    if (movementType === 'Contribution' && amount > dashboard.availableBalance) {
      setError('O aporte não pode superar o valor disponível registrado.')
      return
    }
    if (movementDate < minMovementDate || movementDate > maxMovementDate) {
      setError('Escolha uma data válida dentro do mês selecionado.')
      return
    }

    setBusy(true)
    setError('')
    try {
      await apiRequest('/investments/movements', {
        method: 'POST',
        body: JSON.stringify({ type: movementType, amount, date: movementDate, note: movementNote.trim() || null }),
      })
      setMovementAmount('')
      setMovementNote('')
      setActiveForm(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível registrar a movimentação.')
    } finally { setBusy(false) }
  }

  async function deleteMovement(id: number) {
    setBusy(true)
    setError('')
    try {
      await apiRequest<void>(`/investments/movements/${id}`, { method: 'DELETE' })
      setConfirmDeleteId(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível remover a movimentação.')
    } finally { setBusy(false) }
  }

  return (
    <section className="capital-plan" aria-labelledby="capital-plan-title">
      <div className="capital-plan-heading">
        <div><span className="capital-plan-kicker">Organização do capital</span><h2 id="capital-plan-title">Onde está seu dinheiro</h2></div>
        <p>Disponível e aplicado mostram a posição atual. O limite e os aportes acompanham o mês selecionado; movimentações entre eles não alteram o saldo total.</p>
      </div>

      <div className="capital-plan-metrics">
        <div><span>Disponível</span><strong className={dashboard.availableBalance < 0 ? 'negative' : ''}>{currencyFormatter.format(dashboard.availableBalance)}</strong><small>Saldo total menos valor aplicado</small></div>
        <div><span>Aplicado</span><strong>{currencyFormatter.format(dashboard.investedBalance)}</strong><small>Valor informado, sem rendimentos</small></div>
        <div><span>Separado para gastar</span><strong>{dashboard.monthlySpendingPlan === null ? '—' : currencyFormatter.format(dashboard.monthlySpendingPlan)}</strong><small className={dashboard.monthlySpendingRemaining !== null && dashboard.monthlySpendingRemaining < 0 ? 'negative' : ''}>{dashboard.monthlySpendingRemaining === null ? 'Defina um limite para este mês' : dashboard.monthlySpendingRemaining < 0 ? `${currencyFormatter.format(Math.abs(dashboard.monthlySpendingRemaining))} acima do planejado` : `${currencyFormatter.format(dashboard.monthlySpendingRemaining)} após despesas confirmadas`}</small></div>
        <div><span>Aportes no mês</span><strong>{currencyFormatter.format(dashboard.monthlyContributions)}</strong><small>{dashboard.monthlyWithdrawals > 0 ? `Resgates: ${currencyFormatter.format(dashboard.monthlyWithdrawals)}` : dashboard.monthlyContributions > 0 ? 'Você investiu neste mês' : 'Nenhum aporte registrado'}</small></div>
      </div>

      <div className="capital-plan-actions">
        <button type="button" className={activeForm === 'opening' ? 'active' : ''} onClick={() => openForm('opening')}>{dashboard.openingInvestedAmount > 0 ? 'Ajustar valor inicial' : 'Informar valor já investido'}</button>
        <button type="button" className={activeForm === 'budget' ? 'active' : ''} onClick={() => openForm('budget')}>{dashboard.monthlySpendingPlan === null ? 'Planejar gastos do mês' : 'Ajustar limite do mês'}</button>
        <button type="button" className={activeForm === 'movement' ? 'active' : ''} disabled={isFutureMonth} onClick={() => openForm('movement')}>Registrar aporte ou resgate</button>
      </div>

      {activeForm === 'opening' && (
        <form className="capital-plan-form" onSubmit={(event) => { void saveOpening(event) }}>
          <div><strong>Valor que já estava investido</strong><p>Informe apenas a parte do capital inicial que já estava aplicada antes dos novos aportes. Ela não será somada ao saldo total novamente.</p></div>
          <label>Valor aplicado no início<CurrencyInput value={openingAmount} onValueChange={setOpeningAmount} /></label>
          <div className="capital-plan-form-actions"><button type="button" className="capital-plan-cancel" onClick={() => openForm(null)}>Cancelar</button><button type="submit" className="capital-plan-save" disabled={busy}>{busy ? 'Salvando...' : 'Salvar valor'}</button></div>
        </form>
      )}

      {activeForm === 'budget' && (
        <form className="capital-plan-form" onSubmit={(event) => { void saveBudget(event) }}>
          <div><strong>Limite de gastos deste mês</strong><p>É uma meta para acompanhar despesas, não uma saída de dinheiro nem uma transferência.</p></div>
          <label>Valor separado para gastar<CurrencyInput value={budgetAmount} onValueChange={setBudgetAmount} /></label>
          <div className="capital-plan-form-actions">{dashboard.monthlySpendingPlan !== null && <button type="button" className="capital-plan-cancel" disabled={busy} onClick={() => { void removeBudget() }}>Remover limite</button>}<button type="button" className="capital-plan-cancel" onClick={() => openForm(null)}>Cancelar</button><button type="submit" className="capital-plan-save" disabled={busy}>{busy ? 'Salvando...' : 'Salvar limite'}</button></div>
        </form>
      )}

      {activeForm === 'movement' && (
        <form className="capital-plan-form capital-movement-form" onSubmit={(event) => { void saveMovement(event) }}>
          <div><strong>Movimentação entre disponível e aplicado</strong><p>Aporte reduz o disponível; resgate devolve dinheiro ao disponível. O saldo total não muda.</p></div>
          <fieldset className="capital-movement-types"><legend>Tipo de movimentação</legend><label><input type="radio" name="investment-movement-type" checked={movementType === 'Contribution'} onChange={() => setMovementType('Contribution')} />Aporte</label><label><input type="radio" name="investment-movement-type" checked={movementType === 'Withdrawal'} onChange={() => setMovementType('Withdrawal')} />Resgate</label></fieldset>
          <label>Valor<CurrencyInput value={movementAmount} onValueChange={setMovementAmount} /></label>
          <label>Data<input type="date" lang="pt-BR" min={minMovementDate} max={maxMovementDate} value={movementDate} onChange={(event) => setMovementDate(event.target.value)} required /></label>
          <label className="capital-movement-note">Observação (opcional)<input maxLength={500} value={movementNote} onChange={(event) => setMovementNote(event.target.value)} placeholder="Ex.: aporte para reserva" /></label>
          <div className="capital-plan-form-actions"><button type="button" className="capital-plan-cancel" onClick={() => openForm(null)}>Cancelar</button><button type="submit" className="capital-plan-save" disabled={busy}>{busy ? 'Registrando...' : `Registrar ${movementType === 'Contribution' ? 'aporte' : 'resgate'}`}</button></div>
        </form>
      )}

      {error && <div className="form-error" role="alert">{error}</div>}

      {dashboard.monthlyInvestmentMovements.length > 0 && (
        <div className="capital-plan-history">
          <h3>Movimentações do mês</h3>
          <ul>{dashboard.monthlyInvestmentMovements.map((movement) => (
            <li key={movement.id}>
              <span>{movement.type === 'Contribution' ? 'Aporte' : 'Resgate'} · {new Date(`${movement.date}T12:00:00`).toLocaleDateString('pt-BR')}{movement.note && <small>{movement.note}</small>}</span>
              <strong>{currencyFormatter.format(movement.amount)}</strong>
              {confirmDeleteId === movement.id ? <span className="capital-plan-delete-confirm"><button type="button" disabled={busy} onClick={() => { void deleteMovement(movement.id) }}>Confirmar</button><button type="button" disabled={busy} onClick={() => setConfirmDeleteId(null)}>Manter</button></span> : <button type="button" className="capital-plan-delete" disabled={busy} onClick={() => setConfirmDeleteId(movement.id)}>Remover</button>}
            </li>
          ))}</ul>
        </div>
      )}
    </section>
  )
}
