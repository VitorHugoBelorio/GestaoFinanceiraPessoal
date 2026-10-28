import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { apiRequest } from '../api/client'
import type { AuthSession, OnboardingSetupInput } from '../types/api'

export interface RegistrationDraft {
  name: string
  email: string
  password: string
}

export interface RegistrationSetupDraft {
  initialCapital: string
  employmentType: 'Clt' | 'Pj'
  fixedIncomeEnabled: boolean
  reminderEnabled: boolean
  fixedMonthlyIncome: string
  incomeDayOfMonth: string
  reminderDay: string
}

interface AuthContextValue {
  user: AuthSession | null
  loading: boolean
  registrationDraft: RegistrationDraft | null
  setRegistrationDraft(draft: RegistrationDraft | null): void
  registrationSetupDraft: RegistrationSetupDraft | null
  setRegistrationSetupDraft(draft: RegistrationSetupDraft | null): void
  login(input: { email: string; password: string }): Promise<AuthSession>
  completeRegistration(setup: OnboardingSetupInput): Promise<AuthSession>
  logout(): Promise<void>
  refresh(): Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthSession | null>(null)
  const [registrationDraft, setRegistrationDraft] = useState<RegistrationDraft | null>(null)
  const [registrationSetupDraft, setRegistrationSetupDraft] = useState<RegistrationSetupDraft | null>(null)
  const [loading, setLoading] = useState(true)
  const refresh = useCallback(async () => {
    try { setUser(await apiRequest<AuthSession>('/auth/me')) } catch { setUser(null) } finally { setLoading(false) }
  }, [])

  useEffect(() => { void refresh() }, [refresh])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    loading,
    registrationDraft,
    setRegistrationDraft,
    registrationSetupDraft,
    setRegistrationSetupDraft,
    async login(input) {
      const session = await apiRequest<AuthSession>('/auth/login', { method: 'POST', body: JSON.stringify(input) })
      setUser(session)
      return session
    },
    async completeRegistration(setup) {
      if (!registrationDraft) throw new Error('O cadastro precisa ser preenchido novamente.')
      const session = await apiRequest<AuthSession>('/auth/register-and-onboard', {
        method: 'POST',
        body: JSON.stringify({ account: registrationDraft, setup }),
      })
      setUser(session)
      setRegistrationDraft(null)
      setRegistrationSetupDraft(null)
      return session
    },
    async logout() {
      await apiRequest<void>('/auth/logout', { method: 'POST' })
      setUser(null)
    },
    refresh,
  }), [loading, refresh, registrationDraft, registrationSetupDraft, user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth deve ser usado dentro de AuthProvider.')
  return context
}
