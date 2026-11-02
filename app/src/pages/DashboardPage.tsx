import { useCallback, useEffect, useRef, useState } from 'react'
import { AnimatePresence } from 'motion/react'
import * as m from 'motion/react-m'
import { useLocation, useNavigate } from 'react-router-dom'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingState } from '../components/ui/LoadingState'
import { MonthlyIncomeNotice } from '../features/dashboard/components/MonthlyIncomeNotice'
import { DashboardHeader } from '../features/dashboard/components/DashboardHeader'
import { DashboardSidebar } from '../features/dashboard/components/DashboardSidebar'
import { readDashboardOrigin } from '../features/dashboard/dashboard.navigation'
import { SummaryCards } from '../features/dashboard/components/SummaryCards'
import { EntryActionButton } from '../features/dashboard/components/EntryActionButton'
import { CategoryFilter } from '../features/dashboard/components/CategoryFilter'
import { TransactionTable } from '../features/dashboard/components/TransactionTable'
import { ChartsPanel, type ChartsMode } from '../features/dashboard/components/ChartsPanel'
import { currencyFormatter } from '../features/dashboard/dashboard.formatters'
import type { DashboardTab } from '../features/dashboard/dashboard.types'
import { useSidebarCollapsed } from '../features/dashboard/useSidebarCollapsed'
import { panelVariants } from '../motion/variants'
import type { Category, DashboardData, FinancialTransaction, OnboardingStatus, TransactionType } from '../types/api'
import { isDashboardData, isTransactionList } from '../utils/financialData'

export function DashboardPage() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const initialOrigin = readDashboardOrigin(location.state)
  const today = new Date()
  const [referenceDate, setReferenceDate] = useState(() => initialOrigin ? new Date(initialOrigin.year, initialOrigin.month - 1, 1) : new Date(today.getFullYear(), today.getMonth(), 1))
  const isFutureMonth = referenceDate > new Date(today.getFullYear(), today.getMonth(), 1)
  const [activeTab, setActiveTab] = useState<DashboardTab>(initialOrigin?.tab ?? 'overview')
  const [chartsMode, setChartsMode] = useState<ChartsMode>('monthly')
  const [sidebarCollapsed, setSidebarCollapsed] = useSidebarCollapsed()
  const [dashboard, setDashboard] = useState<DashboardData | null>(null)
  const [transactions, setTransactions] = useState<FinancialTransaction[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [categoryFilter, setCategoryFilter] = useState(initialOrigin?.categoryFilter ?? 'all')
  const [loading, setLoading] = useState(true)
  const [loadedMonth, setLoadedMonth] = useState<string | null>(null)
  const latestRequest = useRef(0)
  const [error, setError] = useState('')
  const [reminderStatus, setReminderStatus] = useState<OnboardingStatus | null>(null)
  const selectedMonth = `${referenceDate.getFullYear()}-${referenceDate.getMonth() + 1}`
  const annualCharts = activeTab === 'charts' && chartsMode === 'annual'
  const updatingMonth = !annualCharts && (loading || (dashboard !== null && loadedMonth !== selectedMonth && !error))
  const monthLabel = referenceDate.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  const historyYear = Math.min(referenceDate.getFullYear(), today.getFullYear())
  const historyEnd = historyYear === today.getFullYear()
    ? `${historyYear}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
    : `${historyYear}-12-31`
  const visibleTransactions = transactions.filter((transaction) => categoryFilter === 'all' || (categoryFilter === 'none' ? transaction.categoryId === null : transaction.categoryId === Number(categoryFilter)))

  const loadData = useCallback(async () => {
    const request = ++latestRequest.current
    const month = `${referenceDate.getFullYear()}-${referenceDate.getMonth() + 1}`
    setLoading(true)
    setError('')
    const query = `?year=${referenceDate.getFullYear()}&month=${referenceDate.getMonth() + 1}`
    try {
      const [dashboardData, transactionData, onboardingData, categoryData] = await Promise.all([
        apiRequest<unknown>(`/dashboard${query}`),
        apiRequest<unknown>(`/transactions${query}`),
        apiRequest<OnboardingStatus>('/onboarding'),
        apiRequest<Category[]>('/categories'),
      ])
      if (!isDashboardData(dashboardData) || !isTransactionList(transactionData)) throw new Error('Dados financeiros incompletos.')
      if (request !== latestRequest.current) return
      setDashboard(dashboardData)
      setTransactions(transactionData)
      setReminderStatus(onboardingData)
      setCategories(categoryData)
      setLoadedMonth(month)
    } catch (requestError) {
      if (request !== latestRequest.current) return
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível carregar o dashboard.')
    } finally { if (request === latestRequest.current) setLoading(false) }
  }, [referenceDate])

  useEffect(() => { if (!annualCharts) void loadData() }, [annualCharts, loadData])

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  function openEntry(type: TransactionType, monthlyIncome = false) {
    navigate(`/app/lancamentos/${type === 'Income' ? 'receita' : 'despesa'}`, {
      state: {
        origin: { tab: activeTab, year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter },
        monthlyIncome,
      },
    })
  }

  function openTransaction(transaction: FinancialTransaction) {
    navigate(`/app/lancamentos/${transaction.id}`, {
      state: { origin: { tab: activeTab, year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter } },
    })
  }

  function openInvestments() {
    navigate('/app/investimentos', { state: { origin: { tab: activeTab, year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter } } })
  }

  function openSection(path: string) {
    navigate(path, { state: { origin: { tab: activeTab, year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter } } })
  }

  function showPreviousMonth() {
    setReferenceDate((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))
  }

  function shiftSelectedPeriod(direction: -1 | 1) {
    setReferenceDate((current) => activeTab === 'charts' && chartsMode === 'annual'
      ? new Date(current.getFullYear() + direction, current.getMonth(), 1)
      : new Date(current.getFullYear(), current.getMonth() + direction, 1))
  }

  function openYearHistory() {
    navigate('/app/periodos', { state: {
      periodSelection: { from: `${historyYear}-01-01`, to: historyEnd, preset: 'custom' },
    } })
  }

  const emptyMonthContent = <>
    <p>Não há lançamentos em {monthLabel}. Esta lista mostra apenas o mês selecionado; consulte outros meses para ver registros anteriores.</p>
    <div className="empty-table-actions"><button type="button" onClick={showPreviousMonth}>Ver mês anterior</button><button type="button" onClick={openYearHistory}>Ver lançamentos de {historyYear}</button></div>
  </>

  return (
    <div className={`app-shell${sidebarCollapsed ? ' is-sidebar-collapsed' : ''}`}>
      <DashboardSidebar
        activeTab={activeTab}
        userName={user?.name ?? ''}
        userEmail={user?.email ?? ''}
        collapsed={sidebarCollapsed}
        onToggleCollapse={() => setSidebarCollapsed((current) => !current)}
        onLogout={() => { void handleLogout() }}
        onTabChange={setActiveTab}
        onInvestmentsClick={openInvestments}
        onPeriodsClick={() => openSection('/app/periodos')}
        onPlanningClick={() => openSection('/app/planejamento')}
      />

      <main className="dashboard-content">
        <DashboardHeader
          referenceDate={referenceDate}
          updating={updatingMonth}
          userName={user?.name ?? ''}
          periodMode={activeTab === 'charts' && chartsMode === 'annual' ? 'year' : 'month'}
          onPreviousPeriod={() => shiftSelectedPeriod(-1)}
          onNextPeriod={() => shiftSelectedPeriod(1)}
        />

        {!annualCharts && loading && !dashboard && <LoadingState label="Carregando seu panorama..." />}
        {!annualCharts && error && <div className="form-error" role="alert">{error} <button className="capital-retry" type="button" onClick={() => { void loadData() }}>Tentar novamente</button></div>}

        <AnimatePresence mode="wait" initial={false}>
        {dashboard && (loadedMonth === selectedMonth || !error) && activeTab === 'overview' && (
          <m.section key="overview" id="dashboard-panel-overview" role="tabpanel" aria-labelledby="dashboard-tab-overview" aria-busy={updatingMonth} inert={updatingMonth} className={`dashboard-view${updatingMonth ? ' is-updating' : ''}`} variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <SummaryCards dashboard={dashboard} />
            <section className="capital-summary" aria-label="Resumo do capital">
              <div><span>Disponível</span><strong>{currencyFormatter.format(dashboard.availableBalance)}</strong></div>
              <div><span>Aplicado</span><strong>{currencyFormatter.format(dashboard.investedBalance)}</strong></div>
              <button type="button" onClick={openInvestments}>Gerenciar investimentos <span aria-hidden="true">↗</span></button>
            </section>
            {dashboard.employmentType === 'Pj' && <MonthlyIncomeNotice dashboard={dashboard} reminderStatus={reminderStatus} isFutureMonth={isFutureMonth} onRegisterIncome={() => openEntry('Income', true)} onReminderUpdated={() => { void loadData() }} />}
            <TransactionTable title="Lançamentos recentes" transactions={dashboard.recentTransactions} emptyMessage={emptyMonthContent} onAddIncome={() => openEntry('Income')} onAddExpense={() => openEntry('Expense')} onViewTransaction={openTransaction} />
          </m.section>
        )}

        {(!error || loadedMonth === selectedMonth) && activeTab === 'transactions' && (
          <m.section key="transactions" id="dashboard-panel-transactions" role="tabpanel" aria-labelledby="dashboard-tab-transactions" aria-busy={updatingMonth} inert={updatingMonth} className={`dashboard-view${updatingMonth ? ' is-updating' : ''}`} variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <div className="section-heading">
              <div><span className="eyebrow">Movimentações</span><h2>Receitas e despesas</h2></div>
              <div className="entry-actions"><EntryActionButton type="Income" label="Lançar receita" onClick={() => openEntry('Income')} /><EntryActionButton type="Expense" label="Lançar despesa" onClick={() => openEntry('Expense')} /></div>
            </div>
            <div className="transaction-filters"><CategoryFilter categories={categories} value={categoryFilter} onChange={setCategoryFilter} /></div>
            <TransactionTable transactions={visibleTransactions} emptyMessage={categoryFilter === 'all' ? emptyMonthContent : <><p>Não há lançamentos com esta etiqueta em {monthLabel}.</p><div className="empty-table-actions"><button type="button" onClick={() => setCategoryFilter('all')}>Mostrar todas as etiquetas</button></div></>} onViewTransaction={openTransaction} />
          </m.section>
        )}

        {activeTab === 'charts' && (
          <m.section key="charts" id="dashboard-panel-charts" role="tabpanel" aria-labelledby="dashboard-tab-charts" aria-busy={updatingMonth} inert={updatingMonth} className={`dashboard-view${updatingMonth ? ' is-updating' : ''}`} variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <ChartsPanel mode={chartsMode} onModeChange={setChartsMode} year={referenceDate.getFullYear()} month={referenceDate.getMonth() + 1} monthlyAvailable={dashboard !== null && (loadedMonth === selectedMonth || !error)} monthlyTransactions={transactions} onSelectMonth={(month) => { setReferenceDate(new Date(referenceDate.getFullYear(), month - 1, 1)); setChartsMode('monthly') }} onPreviousMonth={showPreviousMonth} />
          </m.section>
        )}
        </AnimatePresence>
      </main>
    </div>
  )
}
