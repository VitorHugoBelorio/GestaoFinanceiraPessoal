import { useEffect, useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import { CurrencyInput } from '../../../components/ui/CurrencyInput'
import type { DashboardData, InvestmentAllocation, InvestmentMovementType } from '../../../types/api'
import { parseMoney } from '../../../utils/money'
import { currencyFormatter } from '../dashboard.formatters'
import { InvestmentAllocationPanel } from './InvestmentAllocationPanel'

type CapitalPlanPanelProps = {
  dashboard: DashboardData
  mode?: 'investments' | 'planning'
  onUpdated(): void
  onRegisterIncome?(): void
  allocations?: InvestmentAllocation[]
  initialIncomeContribution?: boolean
}

type ActiveForm = 'budget' | 'movement' | null
type InvestmentStage = 'opening' | 'movement' | 'portfolio'
type BudgetStep = 'amount' | 'carry'

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

export function CapitalPlanPanel({ dashboard, mode = 'investments', onUpdated, onRegisterIncome, allocations = [], initialIncomeContribution = false }: CapitalPlanPanelProps) {
  const [activeForm, setActiveForm] = useState<ActiveForm>(initialIncomeContribution ? 'movement' : null)
  const [investmentStage, setInvestmentStage] = useState<InvestmentStage>(initialIncomeContribution || dashboard.investedBalance > 0 ? 'movement' : 'opening')
  const [budgetStep, setBudgetStep] = useState<BudgetStep>('amount')
  const [openingAmount, setOpeningAmount] = useState(inputMoneyFormatter.format(dashboard.openingInvestedAmount))
  const [budgetAmount, setBudgetAmount] = useState(dashboard.monthlySpendingBaseAmount == null ? '' : inputMoneyFormatter.format(dashboard.monthlySpendingBaseAmount))
  const [carryOverUnused, setCarryOverUnused] = useState(dashboard.monthlySpendingCarryOverUnused)
  const [movementType, setMovementType] = useState<InvestmentMovementType>('Contribution')
  const [movementAmount, setMovementAmount] = useState('')
  const [movementDate, setMovementDate] = useState(defaultMovementDate(dashboard.year, dashboard.month))
  const [movementNote, setMovementNote] = useState('')
  const [movementFromMonthlyIncome, setMovementFromMonthlyIncome] = useState(initialIncomeContribution)
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null)
  const [lastSavedMovement, setLastSavedMovement] = useState<InvestmentMovementType | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const allocatedAmount = Math.round(allocations.reduce((total, allocation) => total + allocation.amount, 0) * 100) / 100
  const today = new Date()
  const isFutureMonth = new Date(dashboard.year, dashboard.month - 1, 1) > new Date(today.getFullYear(), today.getMonth(), 1)
  const maxMovementDate = defaultMovementDate(dashboard.year, dashboard.month)
  const minMovementDate = `${dashboard.year}-${String(dashboard.month).padStart(2, '0')}-01`

  useEffect(() => {
    if (activeForm !== null) return
    setOpeningAmount(inputMoneyFormatter.format(dashboard.openingInvestedAmount))
    setBudgetAmount(dashboard.monthlySpendingBaseAmount == null ? '' : inputMoneyFormatter.format(dashboard.monthlySpendingBaseAmount))
    setCarryOverUnused(dashboard.monthlySpendingCarryOverUnused)
  }, [activeForm, dashboard.openingInvestedAmount, dashboard.monthlySpendingBaseAmount, dashboard.monthlySpendingCarryOverUnused])

  function openForm(form: ActiveForm) {
    setError('')
    setConfirmDeleteId(null)
    if (form === 'budget') setBudgetStep('amount')
    setActiveForm((current) => current === form ? null : form)
  }

  function showInvestmentStage(stage: InvestmentStage) {
    if (busy || stage === investmentStage) return
    setError('')
    setConfirmDeleteId(null)
    setLastSavedMovement(null)
    setActiveForm(null)
    setInvestmentStage(stage)
  }

  function chooseMovement(type: InvestmentMovementType, fromIncome = false) {
    if (busy) return
    if (type !== movementType || fromIncome !== movementFromMonthlyIncome) {
      setMovementAmount('')
      setMovementNote('')
    }
    setMovementType(type)
    setMovementFromMonthlyIncome(fromIncome)
    setError('')
    setConfirmDeleteId(null)
    setLastSavedMovement(null)
    setActiveForm('movement')
  }

  function openIncomeContribution() {
    chooseMovement('Contribution', true)
  }

  function advanceBudget(event: FormEvent) {
    event.preventDefault()
    if (budgetStep === 'carry') {
      void saveBudget(event)
      return
    }
    const amount = parseMoney(budgetAmount)
    if (!Number.isFinite(amount) || amount < 0) {
      setError('Informe um limite mensal válido para continuar.')
      return
    }
    setError('')
    setBudgetStep('carry')
  }

  async function saveOpening(event: FormEvent) {
    event.preventDefault()
    const amount = parseMoney(openingAmount)
    if (!Number.isFinite(amount) || amount < 0) {
      setError('Informe um valor aplicado inicial válido.')
      return
    }
    if (amount > dashboard.initialCapital) {
      setError(`No cadastro, você informou ${currencyFormatter.format(dashboard.initialCapital)} como capital inicial. Aqui, informe apenas quanto desse capital já estava investido naquela data. Não inclua o salário recebido depois: selecione o mês em que o investiu e use “Investir parte da renda do mês” após registrar o recebimento.`)
      return
    }
    if (amount + dashboard.investedBalance - dashboard.openingInvestedAmount < allocatedAmount) {
      setError('Esse ajuste deixaria os grupos da carteira com valores maiores que o total aplicado. Reduza os grupos primeiro.')
      return
    }

    setBusy(true)
    setError('')
    try {
      await apiRequest<void>('/investments/opening-amount', {
        method: 'PUT', body: JSON.stringify({ amount }),
      })
      setActiveForm(null)
      setInvestmentStage('movement')
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
        method: 'PUT', body: JSON.stringify({ year: dashboard.year, month: dashboard.month, amount, carryOverUnused }),
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
    if (movementType === 'Withdrawal' && dashboard.investedBalance - amount < allocatedAmount) {
      setError('Reduza primeiro os valores dos grupos da carteira para liberar o dinheiro que deseja resgatar.')
      return
    }
    if (movementType === 'Contribution' && amount > dashboard.availableBalance) {
      setError('O valor investido não pode superar o saldo disponível registrado.')
      return
    }
    if (movementType === 'Contribution' && movementFromMonthlyIncome && amount > dashboard.monthlyIncome - dashboard.monthlyIncomeInvested) {
      setError('O valor supera a parte da renda recebida neste mês que ainda não foi informada como investimento. Registre o recebimento ou ajuste o valor.')
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
        body: JSON.stringify({ type: movementType, amount, date: movementDate, note: movementNote.trim() || null, isFromMonthlyIncome: movementType === 'Contribution' && movementFromMonthlyIncome }),
      })
      setMovementAmount('')
      setMovementNote('')
      setActiveForm(null)
      setLastSavedMovement(movementType)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError && requestError.status === 400 && movementFromMonthlyIncome
        ? 'Confira se o recebimento foi registrado até a data do investimento e se ainda há renda suficiente neste mês.'
        : requestError instanceof ApiError ? requestError.message : 'Não foi possível registrar a movimentação.')
    } finally { setBusy(false) }
  }

  async function deleteMovement(id: number) {
    const movement = dashboard.monthlyInvestmentMovements.find((item) => item.id === id)
    if (movement?.type === 'Contribution' && dashboard.investedBalance - movement.amount < allocatedAmount) {
      setError('Reduza primeiro os valores dos grupos da carteira antes de remover este investimento.')
      setConfirmDeleteId(null)
      return
    }
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
        <div><span className="capital-plan-kicker">{mode === 'investments' ? 'Organização do capital' : 'Planejamento do mês'}</span><h2 id="capital-plan-title">{mode === 'investments' ? 'Onde está seu dinheiro' : 'Seu limite de gastos'}</h2></div>
        <p>{mode === 'investments' ? 'Disponível e aplicado mostram a posição atual. Movimentações entre eles não alteram o saldo total.' : 'Defina um valor para gastar neste mês e escolha se a sobra entrará no limite do mês seguinte.'}</p>
      </div>

      <div className={`capital-plan-metrics capital-plan-metrics-${mode}`}>
        {mode === 'investments' && <>
        <div><span>Disponível</span><strong className={dashboard.availableBalance < 0 ? 'negative' : ''}>{currencyFormatter.format(dashboard.availableBalance)}</strong><small>Parte do saldo que não está aplicada</small></div>
        <div><span>Investido</span><strong>{currencyFormatter.format(dashboard.investedBalance)}</strong><small>Valor informado por você, sem rendimentos</small></div>
        </>}
        {mode === 'planning' && <>
        <div><span>Limite para gastar</span><strong>{dashboard.monthlySpendingPlan === null ? '—' : currencyFormatter.format(dashboard.monthlySpendingPlan)}</strong><small>{dashboard.monthlySpendingPlan === null ? 'Defina um limite para este mês' : 'Valor planejado para o mês'}</small>{dashboard.monthlySpendingCarryIn > 0 && <small>{dashboard.monthlySpendingPlan === null ? `${currencyFormatter.format(dashboard.monthlySpendingCarryIn)} aguardam um novo limite` : `Inclui ${currencyFormatter.format(dashboard.monthlySpendingCarryIn)} do mês anterior`}</small>}</div>
        <div><span>Despesas confirmadas</span><strong>{currencyFormatter.format(dashboard.monthlyExpense)}</strong><small>Gastos registrados neste mês</small></div>
        <div><span>Ainda pode gastar</span><strong className={dashboard.monthlySpendingRemaining !== null && dashboard.monthlySpendingRemaining < 0 ? 'negative' : 'positive'}>{dashboard.monthlySpendingRemaining === null ? '—' : currencyFormatter.format(Math.max(0, dashboard.monthlySpendingRemaining))}</strong><small className={dashboard.monthlySpendingRemaining !== null && dashboard.monthlySpendingRemaining < 0 ? 'negative' : ''}>{dashboard.monthlySpendingRemaining === null ? 'Defina um limite para ver quanto resta' : dashboard.monthlySpendingRemaining < 0 ? `Você ultrapassou o limite em ${currencyFormatter.format(Math.abs(dashboard.monthlySpendingRemaining))}` : dashboard.monthlySpendingRemaining === 0 ? 'Limite usado por completo' : 'Depois das despesas confirmadas'}</small></div>
        </>}
        {mode === 'investments' &&
        <div><span>Investido neste mês</span><strong>{currencyFormatter.format(dashboard.monthlyContributions)}</strong><small>{dashboard.monthlyIncomeInvested > 0 ? `${currencyFormatter.format(dashboard.monthlyIncomeInvested)} vieram da renda do mês` : dashboard.monthlyContributions > 0 ? 'Origem na renda do mês não informada' : 'Nenhum investimento novo registrado'}</small>{dashboard.monthlyWithdrawals > 0 && <small>Resgates: {currencyFormatter.format(dashboard.monthlyWithdrawals)}</small>}</div>
        }
      </div>

      {mode === 'investments' && dashboard.monthlyIncomeInvested > dashboard.monthlyIncome && <div className="form-error capital-income-mismatch" role="status">O valor informado como investido a partir da renda supera as receitas registradas neste mês. Revise os recebimentos ou remova um investimento incorreto.</div>}

      {mode === 'investments' && <>
        <p className="capital-flow-intro">O que você quer organizar agora? Escolha uma parte; você pode voltar às outras depois.</p>
        <nav className="capital-flow-steps" aria-label="Partes da organização dos investimentos">
          <button type="button" disabled={busy} aria-pressed={investmentStage === 'opening'} className={investmentStage === 'opening' ? 'active' : ''} onClick={() => showInvestmentStage('opening')}><span>01</span><strong>O que eu já tinha</strong><small>Parte do capital inicial que estava investida</small></button>
          <button type="button" disabled={busy} aria-pressed={investmentStage === 'movement'} className={investmentStage === 'movement' ? 'active' : ''} onClick={() => showInvestmentStage('movement')}><span>02</span><strong>O que investi ou resgatei</strong><small>Dinheiro movimentado no mês selecionado</small></button>
          <button type="button" disabled={busy} aria-pressed={investmentStage === 'portfolio'} className={investmentStage === 'portfolio' ? 'active' : ''} onClick={() => showInvestmentStage('portfolio')}><span>03</span><strong>Onde está investido</strong><small>Separe o aplicado em grupos da carteira</small></button>
        </nav>
      </>}

      {mode === 'planning' && activeForm !== 'budget' && <div className="capital-plan-actions"><button type="button" onClick={() => openForm('budget')}>{dashboard.monthlySpendingPlan === null ? 'Definir limite de gastos' : 'Ajustar limite de gastos'}</button></div>}

      {mode === 'investments' && investmentStage === 'movement' && <div className="capital-movement-choice">
        <div className="capital-stage-heading"><strong>O que aconteceu com seu dinheiro?</strong><p>Investir é mover dinheiro do disponível para o aplicado. Resgatar faz o caminho contrário. Nenhuma dessas ações altera o saldo total.</p></div>
        {isFutureMonth ? <p className="capital-future-note">Este mês ainda não chegou. Selecione o mês em que o investimento ou resgate aconteceu para registrar o movimento.</p> : <div className="capital-movement-choice-options">
          <button type="button" disabled={busy} className={activeForm === 'movement' && movementType === 'Contribution' && movementFromMonthlyIncome ? 'active' : ''} aria-pressed={activeForm === 'movement' && movementType === 'Contribution' && movementFromMonthlyIncome} onClick={openIncomeContribution}><strong>Investi parte da renda do mês</strong><small>Use após registrar o salário ou recebimento</small></button>
          <button type="button" disabled={busy} className={activeForm === 'movement' && movementType === 'Contribution' && !movementFromMonthlyIncome ? 'active' : ''} aria-pressed={activeForm === 'movement' && movementType === 'Contribution' && !movementFromMonthlyIncome} onClick={() => chooseMovement('Contribution')}><strong>Investi outro dinheiro</strong><small>Valor que já estava disponível</small></button>
          <button type="button" disabled={busy} className={activeForm === 'movement' && movementType === 'Withdrawal' ? 'active' : ''} aria-pressed={activeForm === 'movement' && movementType === 'Withdrawal'} onClick={() => chooseMovement('Withdrawal')}><strong>Resgatei um investimento</strong><small>O valor voltou a ficar disponível</small></button>
        </div>}
      </div>}

      {mode === 'investments' && investmentStage === 'opening' && (
        <form className="capital-plan-form" onSubmit={(event) => { void saveOpening(event) }}>
          <div className="capital-form-intro"><strong>Quanto você já tinha investido ao começar?</strong><p>Seu capital informado no cadastro foi {currencyFormatter.format(dashboard.initialCapital)}. Informe somente a parte que já estava aplicada naquela data. Se não tinha investimentos, deixe R$ 0,00 e siga para a próxima parte.</p></div>
          <label>Parte investida do capital inicial<CurrencyInput value={openingAmount} onValueChange={setOpeningAmount} /></label>
          <p className="capital-step-note">O salário ou recebimento que entrou depois do cadastro não vai aqui. Na parte 2, você poderá informar quanto investiu dele.</p>
          <div className="capital-plan-form-actions"><button type="button" className="capital-plan-cancel" disabled={busy} onClick={() => showInvestmentStage('movement')}>Pular por enquanto</button><button type="submit" className="capital-plan-save" disabled={busy}>{busy ? 'Salvando...' : 'Salvar e continuar'}</button></div>
        </form>
      )}

      {activeForm === 'budget' && (
        <form className="capital-plan-form capital-budget-form" onSubmit={advanceBudget}>
          <div className="capital-budget-progress" aria-label="Etapa do planejamento"><span className={budgetStep === 'amount' ? 'active' : ''}>1. Definir valor</span><span className={budgetStep === 'carry' ? 'active' : ''}>2. Decidir sobre a sobra</span></div>
          {budgetStep === 'amount' ? <>
            <div className="capital-form-intro"><strong>Quanto pretende gastar neste mês?</strong><p>Escolha um limite para acompanhar os gastos. Esse valor é uma meta; o dinheiro não sai da sua conta quando você o define.</p></div>
            <label>Meu limite para este mês<CurrencyInput value={budgetAmount} onValueChange={setBudgetAmount} /></label>
            {dashboard.monthlySpendingCarryIn > 0 && <p className="capital-rollover-preview">Há {currencyFormatter.format(dashboard.monthlySpendingCarryIn)} de sobra acumulada do mês anterior. Ela será somada ao limite que você definir.</p>}
            <div className="capital-plan-form-actions"><button type="button" className="capital-plan-cancel" onClick={() => openForm(null)}>Cancelar</button><button type="submit" className="capital-plan-save">Continuar</button></div>
          </> : <>
            <div className="capital-form-intro"><strong>Quer levar a sobra para o próximo mês?</strong><p>Você definiu {currencyFormatter.format(parseMoney(budgetAmount))} para este mês{dashboard.monthlySpendingCarryIn > 0 ? `; somando a sobra anterior, seu limite será ${currencyFormatter.format(parseMoney(budgetAmount) + dashboard.monthlySpendingCarryIn)}` : ''}. As despesas confirmadas diminuem o valor que ainda pode gastar.</p></div>
            <label className="capital-rollover-option"><input type="checkbox" checked={carryOverUnused} onChange={(event) => setCarryOverUnused(event.target.checked)} /><span><strong>Acumular o que sobrar</strong><small>Se sobrar dinheiro do limite ao fechar este mês, ele será somado ao novo limite que você definir no mês seguinte. Se desmarcar, o próximo limite começa apenas com o valor que você informar.</small></span></label>
            <p className="capital-rollover-note">A sobra continua no seu saldo disponível nos dois casos. Esta opção não cria receita nem investimento automático.</p>
            <div className="capital-plan-form-actions">{dashboard.monthlySpendingPlan !== null && <button type="button" className="capital-plan-cancel" disabled={busy} onClick={() => { void removeBudget() }}>Remover limite deste mês</button>}<button type="button" className="capital-plan-cancel" disabled={busy} onClick={() => { setError(''); setBudgetStep('amount') }}>Voltar</button><button type="button" className="capital-plan-cancel" disabled={busy} onClick={() => openForm(null)}>Cancelar</button><button type="submit" className="capital-plan-save" disabled={busy}>{busy ? 'Salvando...' : 'Salvar planejamento'}</button></div>
          </>}
        </form>
      )}

      {mode === 'investments' && investmentStage === 'movement' && activeForm === 'movement' && (
        <form className="capital-plan-form capital-movement-form" onSubmit={(event) => { void saveMovement(event) }}>
          <div className="capital-form-intro"><strong>{movementType === 'Withdrawal' ? 'Quanto você resgatou?' : movementFromMonthlyIncome ? 'Quanto da renda recebida você investiu?' : 'Quanto do seu dinheiro disponível você investiu?'}</strong><p>{movementType === 'Withdrawal' ? 'O valor sairá do aplicado e voltará ao disponível. O saldo total não muda.' : 'O valor sairá do disponível e passará ao aplicado. O saldo total não muda e nenhuma receita é criada aqui.'}</p></div>
          {movementType === 'Contribution' && movementFromMonthlyIncome && <p className="capital-income-source-hint">Receitas registradas neste mês: {currencyFormatter.format(dashboard.monthlyIncome)}. Já informados como investidos da renda: {currencyFormatter.format(dashboard.monthlyIncomeInvested)}. {dashboard.monthlyIncome <= 0 ? 'Registre primeiro o salário ou recebimento deste mês.' : 'Esse registro apenas move dinheiro do disponível para o aplicado; não cria outra receita.'} {onRegisterIncome && <button type="button" className="capital-inline-action" onClick={onRegisterIncome}>Registrar salário ou recebimento</button>}</p>}
          <label>{movementType === 'Withdrawal' ? 'Valor resgatado' : 'Valor investido'}<CurrencyInput value={movementAmount} onValueChange={setMovementAmount} /></label>
          <label>Data<input type="date" lang="pt-BR" min={minMovementDate} max={maxMovementDate} value={movementDate} onChange={(event) => setMovementDate(event.target.value)} required /></label>
          <label className="capital-movement-note">Observação (opcional)<input maxLength={500} value={movementNote} onChange={(event) => setMovementNote(event.target.value)} placeholder="Ex.: reserva de emergência" /></label>
          <div className="capital-plan-form-actions"><button type="button" className="capital-plan-cancel" disabled={busy} onClick={() => openForm(null)}>Cancelar</button><button type="submit" className="capital-plan-save" disabled={busy}>{busy ? 'Registrando...' : `Registrar ${movementType === 'Contribution' ? 'investimento' : 'resgate'}`}</button></div>
        </form>
      )}

      {mode === 'investments' && investmentStage === 'movement' && lastSavedMovement && activeForm === null && (
        <div className="capital-next-step" role="status">
          <p>{lastSavedMovement === 'Contribution' ? 'Investimento registrado. O valor já aparece em “Investido”. Se quiser, agora você pode separar esse valor por tipo de investimento.' : 'Resgate registrado. O valor voltou para “Disponível”; seu saldo total não mudou.'}</p>
          {lastSavedMovement === 'Contribution' && <button type="button" onClick={() => showInvestmentStage('portfolio')}>Organizar por grupos <span aria-hidden="true">→</span></button>}
        </div>
      )}

      {error && <div className="form-error" role="alert">{error}</div>}

      {mode === 'investments' && investmentStage === 'movement' && dashboard.monthlyInvestmentMovements.length > 0 && (
        <div className="capital-plan-history">
          <h3>Investimentos e resgates deste mês</h3>
          <ul>{dashboard.monthlyInvestmentMovements.map((movement) => (
            <li key={movement.id}>
              <span>{movement.type === 'Contribution' ? 'Investimento' : 'Resgate'} · {new Date(`${movement.date}T12:00:00`).toLocaleDateString('pt-BR')}{movement.isFromMonthlyIncome && <small>Da renda recebida no mês</small>}{movement.note && <small>{movement.note}</small>}</span>
              <strong>{currencyFormatter.format(movement.amount)}</strong>
              {confirmDeleteId === movement.id ? <span className="capital-plan-delete-confirm"><button type="button" disabled={busy} onClick={() => { void deleteMovement(movement.id) }}>Confirmar</button><button type="button" disabled={busy} onClick={() => setConfirmDeleteId(null)}>Manter</button></span> : <button type="button" className="capital-plan-delete" disabled={busy} onClick={() => setConfirmDeleteId(movement.id)}>Remover</button>}
            </li>
          ))}</ul>
        </div>
      )}
      {mode === 'investments' && investmentStage === 'portfolio' && <InvestmentAllocationPanel allocations={allocations} investedBalance={dashboard.investedBalance} onUpdated={onUpdated} onSetOpening={() => showInvestmentStage('opening')} onAddInvestment={() => showInvestmentStage('movement')} />}
    </section>
  )
}
