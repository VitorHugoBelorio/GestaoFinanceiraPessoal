import { useCallback, useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingState } from '../components/ui/LoadingState'
import { CapitalPlanPanel } from '../features/dashboard/components/CapitalPlanPanel'
import { DashboardIcon } from '../features/dashboard/components/DashboardIcon'
import { DashboardSidebar } from '../features/dashboard/components/DashboardSidebar'
import { readDashboardOrigin } from '../features/dashboard/dashboard.navigation'
import type { DashboardTab } from '../features/dashboard/dashboard.types'
import { useSidebarCollapsed } from '../features/dashboard/useSidebarCollapsed'
import type { DashboardData, InvestmentAllocation } from '../types/api'
import { isDashboardData, isInvestmentAllocationList } from '../utils/financialData'

function CapitalWorkspacePage({ mode }: { mode: 'investments' | 'planning' }) {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const origin = readDashboardOrigin(location.state)
  const [sidebarCollapsed, setSidebarCollapsed] = useSidebarCollapsed()
  const [referenceDate, setReferenceDate] = useState(() => origin
    ? new Date(origin.year, origin.month - 1, 1)
    : new Date(new Date().getFullYear(), new Date().getMonth(), 1))
  const [dashboard, setDashboard] = useState<DashboardData | null>(null)
  const [allocations, setAllocations] = useState<InvestmentAllocation[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const latestRequest = useRef(0)
  const resumeIncomeContribution = useRef(location.state && typeof location.state === 'object' && 'resumeIncomeContribution' in location.state && location.state.resumeIncomeContribution === true)
  const selectedMonth = `${referenceDate.getFullYear()}-${referenceDate.getMonth() + 1}`
  const displayedMonth = dashboard ? `${dashboard.year}-${dashboard.month}` : null
  const updating = loading || displayedMonth !== selectedMonth

  const loadData = useCallback(async () => {
    const request = ++latestRequest.current
    setLoading(true)
    setError('')
    try {
      const [data, allocationData] = await Promise.all([
        apiRequest<unknown>(`/dashboard?year=${referenceDate.getFullYear()}&month=${referenceDate.getMonth() + 1}`),
        mode === 'investments' ? apiRequest<unknown>('/investments/allocations') : Promise.resolve([]),
      ])
      if (!isDashboardData(data, mode === 'planning')) throw new Error('Dados financeiros incompletos.')
      if (!isInvestmentAllocationList(allocationData)) throw new Error('Grupos de investimento incompletos.')
      if (request === latestRequest.current) {
        setDashboard(data)
        setAllocations(allocationData)
      }
    } catch (requestError) {
      if (request === latestRequest.current) {
        setDashboard(null)
        setAllocations([])
        setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível carregar estes dados. Tente novamente.')
      }
    } finally {
      if (request === latestRequest.current) setLoading(false)
    }
  }, [mode, referenceDate])

  useEffect(() => { void loadData() }, [loadData])
  useEffect(() => {
    if (!dashboard || !resumeIncomeContribution.current) return
    resumeIncomeContribution.current = false
    navigate(location.pathname, { replace: true, state: { origin } })
  }, [dashboard, location.pathname, navigate, origin])

  function goToDashboard(tab: DashboardTab) {
    navigate('/app', { state: { origin: { tab, year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter: origin?.categoryFilter ?? 'all' } } })
  }

  function openSection(path: string) {
    navigate(path, { state: { origin: { tab: origin?.tab ?? 'overview', year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter: origin?.categoryFilter ?? 'all' } } })
  }

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <div className={`app-shell${sidebarCollapsed ? ' is-sidebar-collapsed' : ''}`}>
      <DashboardSidebar
        activeTab="overview"
        investmentsActive={mode === 'investments'}
        planningActive={mode === 'planning'}
        userName={user?.name ?? ''}
        userEmail={user?.email ?? ''}
        collapsed={sidebarCollapsed}
        onToggleCollapse={() => setSidebarCollapsed((current) => !current)}
        onLogout={() => { void handleLogout() }}
        onTabChange={goToDashboard}
        onInvestmentsClick={() => { if (mode !== 'investments') openSection('/app/investimentos') }}
        onPlanningClick={() => { if (mode !== 'planning') openSection('/app/planejamento') }}
        onPeriodsClick={() => openSection('/app/periodos')}
      />
      <main className="dashboard-content investments-page">
        <header className="investments-page-header">
          <div>
            <span className="eyebrow">{mode === 'investments' ? 'Organização do capital' : 'Seu orçamento'}</span>
            <h1>{mode === 'investments' ? 'Seus investimentos' : 'Planejamento mensal'}</h1>
            <p>{mode === 'investments' ? 'Veja o que já estava investido, registre o que investiu depois e organize sua carteira. Cada passo trata do mesmo dinheiro, sem somá-lo duas vezes.' : 'Primeiro defina seu limite de gastos. Depois escolha se o que sobrar poderá aumentar o limite do próximo mês.'}</p>
          </div>
          <div className="month-picker" aria-label={mode === 'investments' ? 'Mês das movimentações' : 'Mês do planejamento'}>
            <button type="button" aria-label="Mês anterior" onClick={() => setReferenceDate((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}><DashboardIcon name="chevron-left" /></button>
            <strong>{referenceDate.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })}</strong>
            <button type="button" aria-label="Próximo mês" onClick={() => setReferenceDate((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}><DashboardIcon name="chevron-right" /></button>
            <span className="month-picker-status" role="status">{updating && dashboard ? 'Atualizando mês' : ''}</span>
          </div>
        </header>
        {mode === 'investments' && <p className="investments-page-month-note">O mês selecionado organiza os investimentos e resgates registrados. Os valores “Disponível” e “Investido” mostram a posição atual.</p>}
        {loading && !dashboard && <LoadingState label={mode === 'investments' ? 'Carregando seu capital...' : 'Carregando planejamento...'} />}
        {error && <div className="form-error" role="alert">{error} <button className="capital-retry" type="button" onClick={() => { void loadData() }}>Tentar novamente</button></div>}
        {!error && dashboard && displayedMonth === selectedMonth && (
          <div aria-busy={updating} inert={updating} className={updating ? 'investments-page-updating' : undefined}>
            <CapitalPlanPanel key={`${mode}-${selectedMonth}`} mode={mode} dashboard={dashboard} allocations={allocations} initialIncomeContribution={resumeIncomeContribution.current} onUpdated={() => { void loadData() }} onRegisterIncome={() => navigate('/app/lancamentos/receita', { state: { origin: { tab: 'transactions', year: referenceDate.getFullYear(), month: referenceDate.getMonth() + 1, categoryFilter: 'all' }, returnToInvestments: true } })} />
          </div>
        )}
        <p className="investments-page-footnote">{mode === 'investments' ? <>Os valores investidos são informados por você; rendimentos automáticos ainda não entram nesta conta. <button type="button" onClick={() => openSection('/app/planejamento')}>Ver planejamento mensal</button></> : <>O limite é uma meta de gastos: ele não transfere dinheiro nem faz aportes automaticamente. Se quiser investir parte do disponível, <button type="button" onClick={() => openSection('/app/investimentos')}>vá para investimentos</button>.</>}</p>
      </main>
    </div>
  )
}

export function InvestmentsPage() {
  return <CapitalWorkspacePage mode="investments" />
}

export function MonthlyPlanningPage() {
  return <CapitalWorkspacePage mode="planning" />
}
