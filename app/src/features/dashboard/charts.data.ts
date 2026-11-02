import type { FinancialTransaction } from '../../types/api'

export type CashflowBucket = {
  label: string
  income: number
  expense: number
  count: number
}

export type AnnualBucket = CashflowBucket & {
  month: number
  cumulativeNet: number
  isFuture: boolean
}

function confirmedInPeriod(transactions: FinancialTransaction[], year: number, month?: number) {
  return transactions.filter((transaction) => transaction.status === 'Confirmed'
    && Number(transaction.date.slice(0, 4)) === year
    && (month === undefined || Number(transaction.date.slice(5, 7)) === month))
}

function addTransaction(bucket: CashflowBucket, transaction: FinancialTransaction) {
  if (transaction.type === 'Income') bucket.income += transaction.amount
  else bucket.expense += transaction.amount
  bucket.count += 1
}

export function monthlyCashflow(transactions: FinancialTransaction[], year: number, month: number): CashflowBucket[] {
  const lastDay = new Date(year, month, 0).getDate()
  const buckets = Array.from({ length: Math.ceil(lastDay / 7) }, (_, index) => {
    const first = index * 7 + 1
    return { label: `${first}–${Math.min(first + 6, lastDay)}`, income: 0, expense: 0, count: 0 }
  })
  for (const transaction of confirmedInPeriod(transactions, year, month)) {
    const day = Number(transaction.date.slice(8, 10))
    if (day >= 1 && day <= lastDay) addTransaction(buckets[Math.floor((day - 1) / 7)], transaction)
  }
  return buckets
}

export function annualCashflow(transactions: FinancialTransaction[], year: number, today = new Date()): AnnualBucket[] {
  const buckets: AnnualBucket[] = Array.from({ length: 12 }, (_, index) => ({
    label: new Date(year, index, 1).toLocaleDateString('pt-BR', { month: 'short' }).replace('.', ''),
    month: index + 1,
    income: 0,
    expense: 0,
    count: 0,
    cumulativeNet: 0,
    isFuture: year > today.getFullYear() || (year === today.getFullYear() && index + 1 > today.getMonth() + 1),
  }))
  for (const transaction of confirmedInPeriod(transactions, year)) {
    const month = Number(transaction.date.slice(5, 7))
    if (month >= 1 && month <= 12 && !buckets[month - 1].isFuture) addTransaction(buckets[month - 1], transaction)
  }
  let cumulativeNet = 0
  for (const bucket of buckets) {
    if (bucket.isFuture) continue
    cumulativeNet += bucket.income - bucket.expense
    bucket.cumulativeNet = cumulativeNet
  }
  return buckets
}

export function expenseByCategory(transactions: FinancialTransaction[], year: number, month: number) {
  const totals = new Map<string, number>()
  for (const transaction of confirmedInPeriod(transactions, year, month)) {
    if (transaction.type !== 'Expense') continue
    const name = transaction.categoryName?.trim() || 'Sem etiqueta'
    totals.set(name, (totals.get(name) ?? 0) + transaction.amount)
  }
  const sorted = [...totals].map(([name, amount]) => ({ name, amount })).sort((left, right) => right.amount - left.amount)
  if (sorted.length <= 5) return sorted
  return [...sorted.slice(0, 4), { name: 'Outras etiquetas', amount: sorted.slice(4).reduce((total, item) => total + item.amount, 0) }]
}
