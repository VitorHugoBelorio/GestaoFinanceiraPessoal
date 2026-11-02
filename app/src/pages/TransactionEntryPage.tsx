import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingState } from '../components/ui/LoadingState'
import { DashboardSidebar } from '../features/dashboard/components/DashboardSidebar'
import { TransactionForm } from '../features/dashboard/components/TransactionForm'
import { readDashboardOrigin, readPeriodSelection, isMonthlyIncomeEntry, originForSavedTransaction, periodForSavedTransaction, type DashboardOrigin } from '../features/dashboard/dashboard.navigation'
import { useSidebarCollapsed } from '../features/dashboard/useSidebarCollapsed'
import type { Category, OnboardingStatus, TransactionType } from '../types/api'

export function TransactionEntryPage({ type }: { type: TransactionType }) {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const today = new Date()
  const origin: DashboardOrigin = readDashboardOrigin(location.state) ?? {
    tab: 'transactions', year: today.getFullYear(), month: today.getMonth() + 1, categoryFilter: 'all',
  }
  const [sidebarCollapsed, setSidebarCollapsed] = useSidebarCollapsed()
  const [categories, setCategories] = useState<Category[]>([])
  const [employmentType, setEmploymentType] = useState<OnboardingStatus['employmentType']>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const isIncome = type === 'Income'

  useEffect(() => { window.scrollTo(0, 0) }, [])

  useEffect(() => {
    let active = true
    Promise.all([apiRequest<Category[]>('/categories'), apiRequest<OnboardingStatus>('/onboarding')])
      .then(([categoryData, onboarding]) => {
        if (!active) return
        setCategories(categoryData)
        setEmploymentType(onboarding.employmentType)
      })
      .catch((requestError: unknown) => {
        if (active) setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível preparar o formulário.')
      })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  function returnToDashboard(destination = origin, savedDate?: string) {
    const visibleDestination = savedDate ? originForSavedTransaction({ ...destination, categoryFilter: 'all' }, savedDate) : destination
    if (location.state && typeof location.state === 'object' && 'returnToInvestments' in location.state && location.state.returnToInvestments === true) {
      navigate('/app/investimentos', { replace: true, state: { origin: visibleDestination, resumeIncomeContribution: true } })
      return
    }
    const periodSelection = readPeriodSelection(location.state)
    if (periodSelection) {
      navigate('/app/periodos', { replace: true, state: { periodSelection: savedDate ? periodForSavedTransaction(periodSelection, savedDate) : periodSelection } })
      return
    }
    navigate('/app', { replace: true, state: { origin: visibleDestination } })
  }

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <div className={`app-shell${sidebarCollapsed ? ' is-sidebar-collapsed' : ''}`}>
      <DashboardSidebar
        activeTab="transactions"
        userName={user?.name ?? ''}
        userEmail={user?.email ?? ''}
        collapsed={sidebarCollapsed}
        navigationOnly
        onToggleCollapse={() => setSidebarCollapsed((current) => !current)}
        onLogout={() => { void handleLogout() }}
        onTabChange={(tab) => navigate('/app', { state: { origin: { ...origin, tab } } })}
        onInvestmentsClick={() => navigate('/app/investimentos', { state: { origin } })}
        onPeriodsClick={() => navigate('/app/periodos', { state: { origin, periodSelection: readPeriodSelection(location.state) } })}
        onPlanningClick={() => navigate('/app/planejamento', { state: { origin } })}
      />
      <main className="dashboard-content transaction-entry-page">
        <button className="back-link" type="button" onClick={() => returnToDashboard()}>← Voltar</button>
        <header className="transaction-entry-heading">
          <span className={`eyebrow ${isIncome ? 'entry-tone-income' : 'entry-tone-expense'}`}>Novo lançamento</span>
          <h1>Registrar {isIncome ? 'receita' : 'despesa'}</h1>
          <p>{isIncome ? 'Registre o dinheiro que entrou. Você pode adicionar uma etiqueta e uma observação para identificar a origem.' : 'Registre o dinheiro que saiu. Escolha se a despesa acontece uma vez ou se repete mensalmente por um período.'}</p>
        </header>
        {loading && <LoadingState label="Preparando formulário..." />}
        {error && <div className="form-error" role="alert">{error}</div>}
        {!loading && !error && (
          <TransactionForm
            key={`${type}-${origin.year}-${origin.month}`}
            type={type}
            referenceDate={new Date(origin.year, origin.month - 1, 1)}
            isPj={employmentType === 'Pj'}
            initialMonthlyIncome={isIncome && employmentType === 'Pj' && isMonthlyIncomeEntry(location.state)}
            categories={categories}
            onCategoryCreated={(category) => setCategories((current) => [...current, category].sort((left, right) => left.name.localeCompare(right.name, 'pt-BR')))}
            onCreated={(created) => returnToDashboard(origin, created.date)}
          />
        )}
      </main>
    </div>
  )
}
