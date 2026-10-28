import type { DashboardTab } from './dashboard.types'

export type DashboardOrigin = {
  tab: DashboardTab
  year: number
  month: number
  categoryFilter: string
}

export function readDashboardOrigin(state: unknown): DashboardOrigin | null {
  if (!state || typeof state !== 'object' || !('origin' in state)) return null
  const origin = state.origin
  if (!origin || typeof origin !== 'object' || !('tab' in origin) || !('year' in origin) || !('month' in origin) || !('categoryFilter' in origin)) return null
  if (origin.tab !== 'overview' && origin.tab !== 'transactions' && origin.tab !== 'charts') return null
  if (typeof origin.year !== 'number' || !Number.isInteger(origin.year) || origin.year < 1 || origin.year > 9999) return null
  if (typeof origin.month !== 'number' || !Number.isInteger(origin.month) || origin.month < 1 || origin.month > 12) return null
  if (typeof origin.categoryFilter !== 'string') return null
  return origin as DashboardOrigin
}

export function isMonthlyIncomeEntry(state: unknown): boolean {
  return !!state && typeof state === 'object' && 'monthlyIncome' in state && state.monthlyIncome === true
}
