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
import { CapitalPlanPanel } from '../features/dashboard/components/CapitalPlanPanel'
import { EntryActionButton } from '../features/dashboard/components/EntryActionButton'
import { CategoryFilter } from '../features/dashboard/components/CategoryFilter'
import { TransactionTable } from '../features/dashboard/components/TransactionTable'
import type { DashboardTab } from '../features/dashboard/dashboard.types'
import { useSidebarCollapsed } from '../features/dashboard/useSidebarCollapsed'
import { panelVariants } from '../motion/variants'
import type { Category, DashboardData, FinancialTransaction, OnboardingStatus, TransactionType } from '../types/api'

export function DashboardPage() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const initialOrigin = readDashboardOrigin(location.state)
  const today = new Date()
  const [referenceDate, setReferenceDate] = useState(() => initialOrigin ? new Date(initialOrigin.year, initialOrigin.month - 1, 1) : new Date(today.getFullYear(), today.getMonth(), 1))
  const isFutureMonth = referenceDate > new Date(today.getFullYear(), today.getMonth(), 1)
  const [activeTab, setActiveTab] = useState<DashboardTab>(initialOrigin?.tab ?? 'overview')
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
  const updatingMonth = loading || (dashboard !== null && loadedMonth !== selectedMonth && !error)

  const loadData = useCallback(async () => {
    const request = ++latestRequest.current
    const month = `${referenceDate.getFullYear()}-${referenceDate.getMonth() + 1}`
    setLoading(true)
    setError('')
    const query = `?year=${referenceDate.getFullYear()}&month=${referenceDate.getMonth() + 1}`
    try {
      const [dashboardData, transactionData, onboardingData, categoryData] = await Promise.all([
        apiRequest<DashboardData>(`/dashboard${query}`),
        apiRequest<FinancialTransaction[]>(`/transactions${query}`),
        apiRequest<OnboardingStatus>('/onboarding'),
        apiRequest<Category[]>('/categories'),
      ])
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

  useEffect(() => { void loadData() }, [loadData])

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
      />

      <main className="dashboard-content">
        <DashboardHeader
          referenceDate={referenceDate}
          updating={updatingMonth}
          userName={user?.name ?? ''}
          onPreviousMonth={() => setReferenceDate((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}
          onNextMonth={() => setReferenceDate((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}
        />

        {loading && !dashboard && <LoadingState label="Carregando seu panorama..." />}
        {error && <div className="form-error">{error}</div>}

        <AnimatePresence mode="wait" initial={false}>
        {dashboard && (loadedMonth === selectedMonth || !error) && activeTab === 'overview' && (
          <m.section key="overview" id="dashboard-panel-overview" role="tabpanel" aria-labelledby="dashboard-tab-overview" aria-busy={updatingMonth} inert={updatingMonth} className={`dashboard-view${updatingMonth ? ' is-updating' : ''}`} variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <SummaryCards dashboard={dashboard} />
            <CapitalPlanPanel key={`${dashboard.year}-${dashboard.month}`} dashboard={dashboard} onUpdated={() => { void loadData() }} />
            {dashboard.employmentType === 'Pj' && <MonthlyIncomeNotice dashboard={dashboard} reminderStatus={reminderStatus} isFutureMonth={isFutureMonth} onRegisterIncome={() => openEntry('Income', true)} onReminderUpdated={() => { void loadData() }} />}
            <TransactionTable title="Lançamentos recentes" transactions={dashboard.recentTransactions} onAddIncome={() => openEntry('Income')} onAddExpense={() => openEntry('Expense')} onViewTransaction={openTransaction} />
          </m.section>
        )}

        {(!error || loadedMonth === selectedMonth) && activeTab === 'transactions' && (
          <m.section key="transactions" id="dashboard-panel-transactions" role="tabpanel" aria-labelledby="dashboard-tab-transactions" aria-busy={updatingMonth} inert={updatingMonth} className={`dashboard-view${updatingMonth ? ' is-updating' : ''}`} variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <div className="section-heading">
              <div><span className="eyebrow">Movimentações</span><h2>Receitas e despesas</h2></div>
              <div className="entry-actions"><EntryActionButton type="Income" label="Lançar receita" onClick={() => openEntry('Income')} /><EntryActionButton type="Expense" label="Lançar despesa" onClick={() => openEntry('Expense')} /></div>
            </div>
            <div className="transaction-filters"><CategoryFilter categories={categories} value={categoryFilter} onChange={setCategoryFilter} /></div>
            <TransactionTable transactions={transactions.filter((transaction) => categoryFilter === 'all' || (categoryFilter === 'none' ? transaction.categoryId === null : transaction.categoryId === Number(categoryFilter)))} emptyMessage={categoryFilter === 'all' ? 'Nenhum lançamento neste período.' : 'Nenhum lançamento com essa etiqueta neste período.'} onViewTransaction={openTransaction} />
          </m.section>
        )}

        {activeTab === 'charts' && (
          <m.section key="charts" id="dashboard-panel-charts" role="tabpanel" aria-labelledby="dashboard-tab-charts" className="empty-state" variants={panelVariants} initial="hidden" animate="visible" exit="exit">
            <span className="eyebrow">Próxima etapa</span><h2>Gráficos e análises</h2>
            <p>A estrutura desta aba está reservada. Vamos definir juntos quais comparações e indicadores realmente ajudam na tomada de decisão.</p>
          </m.section>
        )}
        </AnimatePresence>
      </main>
    </div>
  )
}
