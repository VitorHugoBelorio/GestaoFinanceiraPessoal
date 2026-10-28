export type EmploymentType = 'Clt' | 'Pj'
export type IncomeSetupMode = 'Manual' | 'FixedMonthly' | 'MonthlyReminder'
export type TransactionType = 'Income' | 'Expense'
export type TransactionStatus = 'Confirmed' | 'Planned'
export type InvestmentMovementType = 'Contribution' | 'Withdrawal'

export interface Category {
  id: number
  name: string
  type: TransactionType
}

export interface AuthSession {
  userId: number
  name: string
  email: string
  onboardingCompleted: boolean
}

export interface OnboardingSetupInput {
  initialCapital: number
  employmentType: EmploymentType
  incomeSetupMode: IncomeSetupMode
  fixedMonthlyIncome: number | null
  incomeDayOfMonth: number | null
  incomeReminderDay: number | null
}

export interface FinancialTransaction {
  id: number
  type: TransactionType
  description: string
  note: string | null
  amount: number
  date: string
  status: TransactionStatus
  categoryId: number | null
  categoryName: string | null
  isRecurring: boolean
  isInstallment: boolean
  installmentNumber: number | null
  installmentCount: number | null
  totalPurchaseAmount: number | null
  isMonthlyIncome: boolean
  recurrenceStartDate: string | null
  recurrenceEndDate: string | null
}

export interface InvestmentMovement {
  id: number
  type: InvestmentMovementType
  amount: number
  date: string
  note: string | null
}

export interface DashboardData {
  year: number
  month: number
  totalBalance: number
  monthlyIncome: number
  monthlyExpense: number
  monthlyBalance: number
  incomeReminderDue: boolean
  monthlyIncomeRecorded: boolean
  employmentType: EmploymentType
  recentTransactions: FinancialTransaction[]
  initialCapital: number
  openingInvestedAmount: number
  investedBalance: number
  availableBalance: number
  monthlySpendingPlan: number | null
  monthlySpendingRemaining: number | null
  monthlyContributions: number
  monthlyWithdrawals: number
  monthlyInvestmentMovements: InvestmentMovement[]
}

export interface OnboardingStatus {
  completed: boolean
  initialCapital: number | null
  employmentType: EmploymentType | null
  incomeSetupMode: IncomeSetupMode | null
  monthlyIncomeReminderDay: number | null
}
