import type { ReactNode } from 'react'
import { AnimatePresence } from 'motion/react'
import * as m from 'motion/react-m'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import { LoadingState } from './components/ui/LoadingState'
import { routeVariants } from './motion/variants'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { OnboardingPage } from './pages/OnboardingPage'
import { RegisterPage } from './pages/RegisterPage'
import { TransactionEntryPage } from './pages/TransactionEntryPage'
import { TransactionDetailPage } from './pages/TransactionDetailPage'
import { WelcomePage } from './pages/WelcomePage'

function RequireAuth({ children, onboardingRequired = false }: { children: ReactNode; onboardingRequired?: boolean }) {
  const { user, loading } = useAuth()
  if (loading) return <LoadingState label="Preparando sua experiência..." />
  if (!user) return <Navigate to="/acesso" replace />
  if (onboardingRequired && !user.onboardingCompleted) return <Navigate to="/primeiros-passos" replace />
  return children
}

function RequireOnboardingAccess({ children }: { children: ReactNode }) {
  const { user, loading, registrationDraft } = useAuth()
  if (loading) return <LoadingState label="Preparando sua experiência..." />
  if (user?.onboardingCompleted) return <Navigate to="/app" replace />
  if (!user && !registrationDraft) return <Navigate to="/cadastro" replace />
  return children
}

export function App() {
  const location = useLocation()

  return (
    <AnimatePresence mode="wait" initial={false}>
      <m.div
        key={location.pathname}
        className="route-stage"
        variants={routeVariants}
        initial="hidden"
        animate="visible"
        exit="exit"
      >
        <Routes location={location}>
          <Route path="/" element={<WelcomePage />} />
          <Route path="/acesso" element={<LoginPage />} />
          <Route path="/cadastro" element={<RegisterPage />} />
          <Route path="/primeiros-passos" element={<RequireOnboardingAccess><OnboardingPage /></RequireOnboardingAccess>} />
          <Route path="/app" element={<RequireAuth onboardingRequired><DashboardPage /></RequireAuth>} />
          <Route path="/app/lancamentos/receita" element={<RequireAuth onboardingRequired><TransactionEntryPage type="Income" /></RequireAuth>} />
          <Route path="/app/lancamentos/despesa" element={<RequireAuth onboardingRequired><TransactionEntryPage type="Expense" /></RequireAuth>} />
          <Route path="/app/lancamentos/:id" element={<RequireAuth onboardingRequired><TransactionDetailPage /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </m.div>
    </AnimatePresence>
  )
}
