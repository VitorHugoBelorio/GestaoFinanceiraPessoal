import test from 'node:test'
import assert from 'node:assert/strict'
import { originForSavedTransaction, periodForSavedTransaction } from '../src/features/dashboard/dashboard.navigation.ts'
import { annualCashflow, expenseByCategory, monthlyCashflow } from '../src/features/dashboard/charts.data.ts'
import { isDashboardData, isInvestmentAllocationList, isTransactionDetail, isTransactionList } from '../src/utils/financialData.ts'

const validDashboard = {
  year: 2026,
  month: 12,
  totalBalance: 5000,
  monthlyIncome: 2000,
  monthlyPrimaryIncome: 1500,
  monthlyExpense: 1300,
  monthlyBalance: 700,
  initialCapital: 3000,
  openingInvestedAmount: 1000,
  investedBalance: 1200,
  availableBalance: 3800,
  monthlyContributions: 200,
  monthlyIncomeInvested: 0,
  monthlyWithdrawals: 0,
  monthlySpendingPlan: null,
  monthlySpendingRemaining: null,
  monthlySpendingBaseAmount: null,
  monthlySpendingCarryIn: 0,
  monthlySpendingCarryOverUnused: false,
  recentTransactions: [],
  monthlyInvestmentMovements: [],
}

test('planejamento não aceita campos ausentes da resposta da API', () => {
  assert.equal(isDashboardData(validDashboard, true), true)
  const olderResponse = { ...validDashboard }
  delete olderResponse.monthlySpendingBaseAmount
  delete olderResponse.monthlySpendingCarryIn
  delete olderResponse.monthlySpendingCarryOverUnused
  assert.equal(isDashboardData(olderResponse, true), false)
  assert.equal(isDashboardData(olderResponse), true)
})

test('valores financeiros inválidos não chegam aos formatadores', () => {
  assert.equal(isDashboardData({ ...validDashboard, monthlySpendingBaseAmount: Number.NaN }, true), false)
  assert.equal(isDashboardData({ ...validDashboard, availableBalance: undefined }), false)
  assert.equal(isDashboardData({ ...validDashboard, monthlyPrimaryIncome: undefined }), false)
  assert.equal(isDashboardData({ ...validDashboard, monthlyIncomeInvested: undefined }), false)
  assert.equal(isDashboardData({ ...validDashboard, monthlyInvestmentMovements: [{ amount: 100, isFromMonthlyIncome: undefined }] }), false)
  assert.equal(isTransactionList([{ amount: undefined, type: 'Income', status: 'Confirmed' }]), false)
  assert.equal(isTransactionList([{ amount: 100, type: 'Income', status: 'Confirmed' }]), true)
  assert.equal(isTransactionDetail({ amount: 100, type: 'Income', status: 'Confirmed', description: 'Salário', date: '2026-10-30', totalPurchaseAmount: null }), true)
  assert.equal(isTransactionDetail({ amount: 100, type: 'Income', status: 'Confirmed', description: 'Salário', date: '2026-02-31', totalPurchaseAmount: null }), false)
  assert.equal(isTransactionDetail({ amount: 100, type: 'Income', status: 'Confirmed', description: 'Salário', date: '2026-10-30', totalPurchaseAmount: undefined }), false)
  assert.equal(isInvestmentAllocationList([{ id: 1, name: 'CDB', amount: 500 }]), true)
  assert.equal(isInvestmentAllocationList([{ id: 1, name: 'CDB', amount: Number.NaN }]), false)
})

test('um lançamento salvo em outro mês volta para um período onde pode ser encontrado', () => {
  const origin = { tab: 'transactions', year: 2026, month: 11, categoryFilter: 'all' }
  const selection = { from: '2026-11-01', to: '2026-11-30', preset: 'month' }
  assert.deepEqual(originForSavedTransaction(origin, '2026-10-28'), { ...origin, month: 10 })
  assert.deepEqual(periodForSavedTransaction(selection, '2026-10-28'), { from: '2026-10-01', to: '2026-10-31', preset: 'custom' })
  assert.equal(periodForSavedTransaction(selection, '2026-11-02'), selection)
  assert.equal(originForSavedTransaction(origin, '2026-02-31'), origin)
})

test('gráficos mensais e anuais usam apenas lançamentos confirmados no período', () => {
  const entries = [
    { date: '2026-10-28', status: 'Confirmed', type: 'Income', amount: 7560, categoryName: null },
    { date: '2026-10-28', status: 'Confirmed', type: 'Income', amount: 500, categoryName: null },
    { date: '2026-10-08', status: 'Confirmed', type: 'Expense', amount: 200, categoryName: 'Luz' },
    { date: '2026-11-01', status: 'Planned', type: 'Expense', amount: 100, categoryName: 'Luz' },
    { date: '2026-12-01', status: 'Confirmed', type: 'Income', amount: 900, categoryName: null },
  ]
  const weeks = monthlyCashflow(entries, 2026, 10)
  assert.equal(weeks[1].expense, 200)
  assert.equal(weeks[3].income, 8060)
  assert.deepEqual(expenseByCategory(entries, 2026, 10), [{ name: 'Luz', amount: 200 }])

  const months = annualCashflow(entries, 2026, new Date(2026, 10, 2))
  assert.equal(months[9].income, 8060)
  assert.equal(months[9].expense, 200)
  assert.equal(months[9].cumulativeNet, 7860)
  assert.equal(months[10].count, 0)
  assert.equal(months[11].isFuture, true)
  assert.equal(months[11].income, 0)
})
