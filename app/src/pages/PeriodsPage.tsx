import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingState } from '../components/ui/LoadingState'
import { CategoryFilter } from '../features/dashboard/components/CategoryFilter'
import { DashboardSidebar } from '../features/dashboard/components/DashboardSidebar'
import { TransactionTable } from '../features/dashboard/components/TransactionTable'
import { currencyFormatter } from '../features/dashboard/dashboard.formatters'
import { readDashboardOrigin, readPeriodSelection, type PeriodSelection } from '../features/dashboard/dashboard.navigation'
import type { DashboardTab } from '../features/dashboard/dashboard.types'
import { useSidebarCollapsed } from '../features/dashboard/useSidebarCollapsed'
import type { Category, FinancialTransaction, TransactionType } from '../types/api'
import { isTransactionList } from '../utils/financialData'

type Preset = 'today' | 'last7' | 'last30' | 'month' | 'previousMonth' | 'year' | 'custom'

function localDate(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function oneYearAhead(): string {
  const today = new Date()
  const year = today.getFullYear() + 1
  const month = today.getMonth()
  const lastDay = new Date(year, month + 1, 0).getDate()
  return localDate(new Date(year, month, Math.min(today.getDate(), lastDay)))
}

function presetSelection(preset: Preset, referenceMonth?: { year: number; month: number }): PeriodSelection {
  const today = new Date()
  if (preset === 'month' && referenceMonth) return {
    from: localDate(new Date(referenceMonth.year, referenceMonth.month - 1, 1)),
    to: localDate(new Date(referenceMonth.year, referenceMonth.month, 0)),
    preset,
  }
  if (preset === 'previousMonth') return { from: localDate(new Date(today.getFullYear(), today.getMonth() - 1, 1)), to: localDate(new Date(today.getFullYear(), today.getMonth(), 0)), preset }
  if (preset === 'year') return { from: localDate(new Date(today.getFullYear(), 0, 1)), to: localDate(new Date(today.getFullYear(), 11, 31)), preset }
  if (preset === 'month') return { from: localDate(new Date(today.getFullYear(), today.getMonth(), 1)), to: localDate(new Date(today.getFullYear(), today.getMonth() + 1, 0)), preset }
  const start = new Date(today)
  if (preset === 'last7') start.setDate(start.getDate() - 6)
  if (preset === 'last30') start.setDate(start.getDate() - 29)
  return { from: localDate(start), to: localDate(today), preset }
}

function displayDate(value: string): string {
  return new Date(`${value}T12:00:00`).toLocaleDateString('pt-BR')
}

export function PeriodsPage() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const origin = readDashboardOrigin(location.state)
  const savedSelection = readPeriodSelection(location.state)
  const [sidebarCollapsed, setSidebarCollapsed] = useSidebarCollapsed()
  const [selection, setSelection] = useState<PeriodSelection>(() => {
    if (savedSelection) return savedSelection
    const initial = presetSelection('month', origin ?? undefined)
    const today = new Date()
    return origin && (origin.year !== today.getFullYear() || origin.month !== today.getMonth() + 1)
      ? { ...initial, preset: 'custom' }
      : initial
  })
  const [draftFrom, setDraftFrom] = useState(selection.from)
  const [draftTo, setDraftTo] = useState(selection.to)
  const [transactions, setTransactions] = useState<FinancialTransaction[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [categoryFilter, setCategoryFilter] = useState('all')
  const [loading, setLoading] = useState(true)
  const [hasLoaded, setHasLoaded] = useState(false)
  const [error, setError] = useState('')
  const [filterError, setFilterError] = useState('')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError('')
    const query = new URLSearchParams({ from: selection.from, to: selection.to })
    Promise.all([
      apiRequest<unknown>(`/transactions?${query}`),
      apiRequest<Category[]>('/categories'),
    ]).then(([entries, categoryData]) => {
      if (!active) return
      if (!isTransactionList(entries)) throw new Error('Dados financeiros incompletos.')
      setTransactions(entries)
      setCategories(categoryData)
      setHasLoaded(true)
    }).catch((requestError: unknown) => {
      if (active) setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível consultar este período.')
    }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [selection.from, selection.to, retryKey])

  const filteredTransactions = useMemo(() => transactions.filter((transaction) => categoryFilter === 'all' || (categoryFilter === 'none' ? transaction.categoryId === null : transaction.categoryId === Number(categoryFilter))), [transactions, categoryFilter])

  const totals = useMemo(() => {
    const confirmed = filteredTransactions.filter((transaction) => transaction.status === 'Confirmed')
    const income = confirmed.filter((transaction) => transaction.type === 'Income').reduce((sum, transaction) => sum + transaction.amount, 0)
    const expense = confirmed.filter((transaction) => transaction.type === 'Expense').reduce((sum, transaction) => sum + transaction.amount, 0)
    return { income, expense, balance: income - expense, planned: filteredTransactions.length - confirmed.length }
  }, [filteredTransactions])

  const maxDate = oneYearAhead()
  const navigationState = { origin: { tab: 'transactions' as DashboardTab, year: Number(selection.to.slice(0, 4)), month: Number(selection.to.slice(5, 7)), categoryFilter: 'all' }, periodSelection: selection }

  function choosePreset(preset: Preset) {
    const next = presetSelection(preset)
    setSelection(next)
    setDraftFrom(next.from)
    setDraftTo(next.to)
    setFilterError('')
  }

  function applyCustom(event: FormEvent) {
    event.preventDefault()
    const first = new Date(`${draftFrom}T12:00:00`)
    const last = new Date(`${draftTo}T12:00:00`)
    const calendarDays = (Date.UTC(last.getFullYear(), last.getMonth(), last.getDate()) - Date.UTC(first.getFullYear(), first.getMonth(), first.getDate())) / 86400000
    if (Number.isNaN(first.getTime()) || Number.isNaN(last.getTime()) || localDate(first) !== draftFrom || localDate(last) !== draftTo || draftFrom > draftTo || first.getFullYear() < 2000 || draftTo > maxDate || calendarDays > 365) {
      setFilterError('Escolha um intervalo válido de até 366 dias, com data final no máximo um ano à frente.')
      return
    }
    setFilterError('')
    setSelection({ from: draftFrom, to: draftTo, preset: 'custom' })
  }

  function goToDashboard(tab: DashboardTab) {
    navigate('/app', { state: { origin: { tab, year: Number(selection.to.slice(0, 4)), month: Number(selection.to.slice(5, 7)), categoryFilter: 'all' } } })
  }

  function openEntry(type: TransactionType) {
    navigate(`/app/lancamentos/${type === 'Income' ? 'receita' : 'despesa'}`, { state: navigationState })
  }

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <div className={`app-shell${sidebarCollapsed ? ' is-sidebar-collapsed' : ''}`}>
      <DashboardSidebar
        activeTab="overview"
        periodsActive
        userName={user?.name ?? ''}
        userEmail={user?.email ?? ''}
        collapsed={sidebarCollapsed}
        onToggleCollapse={() => setSidebarCollapsed((current) => !current)}
        onLogout={() => { void handleLogout() }}
        onTabChange={goToDashboard}
        onPeriodsClick={() => undefined}
        onPlanningClick={() => navigate('/app/planejamento', { state: navigationState })}
        onInvestmentsClick={() => navigate('/app/investimentos', { state: navigationState })}
      />
      <main className="dashboard-content periods-page">
        <header className="periods-heading"><span className="eyebrow">Análise financeira</span><h1>Explore seus períodos</h1><p>Consulte entradas e saídas por dia, semanas, mês, ano ou pelo intervalo que fizer sentido para você. O orçamento continua mensal na área de planejamento.</p></header>
        <section className="period-filter" aria-label="Escolher período">
          <div className="period-presets">
            {([['today', 'Hoje'], ['last7', 'Últimos 7 dias'], ['last30', 'Últimos 30 dias'], ['month', 'Mês atual'], ['previousMonth', 'Mês anterior'], ['year', 'Este ano']] as const).map(([preset, label]) => (
              <button key={preset} type="button" className={selection.preset === preset ? 'active' : ''} aria-pressed={selection.preset === preset} onClick={() => choosePreset(preset)}>{label}</button>
            ))}
          </div>
          <form className="period-custom" onSubmit={applyCustom}>
            <label>De<input type="date" min="2000-01-01" max="2200-12-31" value={draftFrom} onChange={(event) => setDraftFrom(event.target.value)} required /></label>
            <label>Até<input type="date" min="2000-01-01" max={maxDate} value={draftTo} onChange={(event) => setDraftTo(event.target.value)} required /></label>
            <button type="submit">Aplicar período</button>
          </form>
          {filterError && <p className="period-filter-error" role="alert">{filterError}</p>}
        </section>
        <div className="period-results-heading"><div><span className="eyebrow">Resultado confirmado</span><h2>{displayDate(selection.from)} a {displayDate(selection.to)}</h2></div><span role="status">{loading ? 'Atualizando período...' : `${filteredTransactions.length} lançamento${filteredTransactions.length === 1 ? '' : 's'}`}</span></div>
        {loading && !hasLoaded && <LoadingState label="Consultando lançamentos..." />}
        {error && <div className="form-error" role="alert">{error} <button className="capital-retry" type="button" onClick={() => setRetryKey((current) => current + 1)}>Tentar novamente</button></div>}
        {hasLoaded && !error && <div className={loading ? 'period-results is-updating' : 'period-results'} aria-busy={loading} inert={loading}>
          <div className="period-summary">
            <div><span>Receitas</span><strong className="positive">{currencyFormatter.format(totals.income)}</strong></div>
            <div><span>Despesas</span><strong className="negative">{currencyFormatter.format(totals.expense)}</strong></div>
            <div><span>Resultado</span><strong className={totals.balance >= 0 ? 'positive' : 'negative'}>{currencyFormatter.format(totals.balance)}</strong></div>
          </div>
          {totals.planned > 0 && <p className="period-planned-note">{totals.planned} lançamento{totals.planned === 1 ? ' previsto não entra' : 's previstos não entram'} nos totais acima.</p>}
          <div className="transaction-filters"><CategoryFilter categories={categories} value={categoryFilter} onChange={setCategoryFilter} /></div>
          <TransactionTable title="Lançamentos do período" transactions={filteredTransactions} onAddIncome={() => openEntry('Income')} onAddExpense={() => openEntry('Expense')} onViewTransaction={(transaction) => navigate(`/app/lancamentos/${transaction.id}`, { state: navigationState })} />
        </div>}
      </main>
    </div>
  )
}
