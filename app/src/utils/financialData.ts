import type { DashboardData, FinancialTransaction, InvestmentAllocation } from '../types/api'

function isMoney(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value)
}

function isOptionalMoney(value: unknown): value is number | null {
  return value === null || isMoney(value)
}

export function isInvestmentAllocationList(value: unknown): value is InvestmentAllocation[] {
  return Array.isArray(value) && value.every((entry) => entry !== null && typeof entry === 'object'
    && Number.isInteger(entry.id) && entry.id > 0
    && typeof entry.name === 'string' && entry.name.length > 0
    && isMoney(entry.amount) && entry.amount > 0)
}

export function isTransactionList(value: unknown): value is FinancialTransaction[] {
  return Array.isArray(value) && value.every((entry) =>
    entry !== null && typeof entry === 'object'
    && isMoney(entry.amount)
    && (entry.type === 'Income' || entry.type === 'Expense')
    && (entry.status === 'Confirmed' || entry.status === 'Planned'))
}

export function isTransactionDetail(value: unknown): value is FinancialTransaction {
  if (!isTransactionList([value])) return false
  const entry = value as Record<string, unknown>
  if (typeof entry.description !== 'string' || typeof entry.date !== 'string'
    || !/^\d{4}-\d{2}-\d{2}$/.test(entry.date)
    || !isOptionalMoney(entry.totalPurchaseAmount)) return false

  const date = new Date(`${entry.date}T12:00:00`)
  return !Number.isNaN(date.getTime())
    && `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` === entry.date
}

export function isDashboardData(value: unknown, requirePlanning = false): value is DashboardData {
  if (value === null || typeof value !== 'object') return false
  const data = value as Record<string, unknown>
  const numericFields = [
    'totalBalance', 'monthlyIncome', 'monthlyPrimaryIncome', 'monthlyExpense', 'monthlyBalance',
    'initialCapital', 'openingInvestedAmount', 'investedBalance',
    'availableBalance', 'monthlyContributions', 'monthlyWithdrawals',
    'monthlyIncomeInvested',
  ]

  if (!isMoney(data.year) || !Number.isInteger(data.year)
    || !isMoney(data.month) || !Number.isInteger(data.month)
    || !numericFields.every((field) => isMoney(data[field]))
    || !isOptionalMoney(data.monthlySpendingPlan)
    || !isOptionalMoney(data.monthlySpendingRemaining)
    || !isTransactionList(data.recentTransactions)
    || !Array.isArray(data.monthlyInvestmentMovements)
    || !data.monthlyInvestmentMovements.every((entry) => entry !== null && typeof entry === 'object' && isMoney(entry.amount) && typeof entry.isFromMonthlyIncome === 'boolean')) return false

  return !requirePlanning || (
    isOptionalMoney(data.monthlySpendingBaseAmount)
    && isMoney(data.monthlySpendingCarryIn)
    && typeof data.monthlySpendingCarryOverUnused === 'boolean'
  )
}
