import type { DashboardTab } from './dashboard.types'

export type DashboardOrigin = {
  tab: DashboardTab
  year: number
  month: number
  categoryFilter: string
}

export type PeriodSelection = {
  from: string
  to: string
  preset: string
}

function savedTransactionMonth(date: string): { year: number; month: number } | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date)
  if (!match) return null
  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  if (year < 2000 || year > 2200 || month < 1 || month > 12 || day < 1 || day > new Date(year, month, 0).getDate()) return null
  return { year, month }
}

export function originForSavedTransaction(origin: DashboardOrigin, date: string): DashboardOrigin {
  const month = savedTransactionMonth(date)
  return month ? { ...origin, ...month } : origin
}

export function periodForSavedTransaction(selection: PeriodSelection, date: string): PeriodSelection {
  if (date >= selection.from && date <= selection.to) return selection
  const savedMonth = savedTransactionMonth(date)
  if (!savedMonth) return selection
  const { year, month } = savedMonth
  const prefix = `${year}-${String(month).padStart(2, '0')}`
  return { from: `${prefix}-01`, to: `${prefix}-${String(new Date(year, month, 0).getDate()).padStart(2, '0')}`, preset: 'custom' }
}

export function readPeriodSelection(state: unknown): PeriodSelection | null {
  if (!state || typeof state !== 'object' || !('periodSelection' in state)) return null
  const selection = state.periodSelection
  if (!selection || typeof selection !== 'object' || !('from' in selection) || !('to' in selection) || !('preset' in selection)) return null
  if (typeof selection.from !== 'string' || typeof selection.to !== 'string' || typeof selection.preset !== 'string') return null
  if (!/^\d{4}-\d{2}-\d{2}$/.test(selection.from) || !/^\d{4}-\d{2}-\d{2}$/.test(selection.to)) return null
  return selection as PeriodSelection
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
