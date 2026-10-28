import { useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import type { Category, TransactionType } from '../../../types/api'
import { formatMoneyInput, parseMoney } from '../../../utils/money'
import { currencyFormatter } from '../dashboard.formatters'
import { CategoryPicker } from './CategoryPicker'

type RecurrenceMode = 'none' | 'monthly' | 'installments'

type TransactionFormProps = {
  referenceDate: Date
  isPj?: boolean
  type: TransactionType
  initialMonthlyIncome?: boolean
  categories: Category[]
  onCategoryCreated(category: Category): void
  onCreated(): void
}

export function TransactionForm({ referenceDate, isPj = false, type, initialMonthlyIncome = false, categories, onCategoryCreated, onCreated }: TransactionFormProps) {
  const today = new Date()
  const todayIso = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
  const [description, setDescription] = useState('')
  const [note, setNote] = useState('')
  const [amount, setAmount] = useState('')
  const [date, setDate] = useState(`${referenceDate.getFullYear()}-${String(referenceDate.getMonth() + 1).padStart(2, '0')}-${String(Math.min(today.getDate(), new Date(referenceDate.getFullYear(), referenceDate.getMonth() + 1, 0).getDate())).padStart(2, '0')}`)
  const [recurrenceMode, setRecurrenceMode] = useState<RecurrenceMode>('none')
  const [installmentEndMonth, setInstallmentEndMonth] = useState('')
  const [totalPurchaseAmount, setTotalPurchaseAmount] = useState('')
  const [isMonthlyIncome, setIsMonthlyIncome] = useState(initialMonthlyIncome)
  const [categoryId, setCategoryId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const isInstallment = type === 'Expense' && recurrenceMode === 'installments'
  const [startYear, startMonth] = date.split('-').map(Number)
  const [endYear, endMonth] = installmentEndMonth.split('-').map(Number)
  const installmentCount = isInstallment && date && installmentEndMonth
    ? (endYear - startYear) * 12 + endMonth - startMonth + 1
    : 0
  const installmentEndDate = isInstallment && installmentEndMonth
    ? `${installmentEndMonth}-${String(new Date(endYear, endMonth, 0).getDate()).padStart(2, '0')}`
    : null

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (!amount || !Number.isFinite(parseMoney(amount)) || parseMoney(amount) <= 0) {
      setError('Informe um valor maior que zero.')
      return
    }
    if (isInstallment && installmentCount < 2) {
      setError('Escolha o mês do último lançamento. A repetição precisa durar pelo menos dois meses.')
      return
    }
    if (isInstallment && totalPurchaseAmount && parseMoney(totalPurchaseAmount) <= 0) {
      setError('O valor total informado precisa ser maior que zero.')
      return
    }
    setSubmitting(true)

    try {
      await apiRequest('/transactions', {
        method: 'POST',
        body: JSON.stringify({
          type,
          description,
          note: note.trim() || null,
          amount: parseMoney(amount),
          date,
          status: 'Confirmed',
          categoryId,
          isRecurring: recurrenceMode !== 'none',
          isInstallment,
          totalPurchaseAmount: isInstallment && totalPurchaseAmount ? parseMoney(totalPurchaseAmount) : null,
          recurrenceEndDate: installmentEndDate,
          isMonthlyIncome: isPj && type === 'Income' && isMonthlyIncome,
          recurrenceDayOfMonth: recurrenceMode === 'monthly' ? Number(date.slice(-2)) : null,
        }),
      })
      onCreated()
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status >= 500) {
        setError(`Não foi possível salvar ${type === 'Income' ? 'a receita' : 'a despesa'} agora. Tente novamente em instantes.`)
      } else {
        setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível salvar o lançamento. Tente novamente.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="transaction-form transaction-entry-form" onSubmit={handleSubmit}>
      <label className="transaction-description">Descrição<input value={description} onChange={(event) => setDescription(event.target.value)} required /></label>
      <label>{isInstallment ? 'Valor mensal' : 'Valor'}<input inputMode="numeric" value={amount} onChange={(event) => setAmount(formatMoneyInput(event.target.value))} placeholder="0,00" required /></label>
      <label>{isInstallment ? 'Data do primeiro lançamento' : 'Data'}<input type="date" lang="pt-BR" value={date} max={isMonthlyIncome ? todayIso : undefined} onChange={(event) => setDate(event.target.value)} required /></label>
      <CategoryPicker key={type} type={type} categories={categories} value={categoryId} onChange={setCategoryId} onCreated={onCategoryCreated} />
      <label className="transaction-note-field">Observação (opcional)<textarea value={note} onChange={(event) => setNote(event.target.value)} maxLength={1000} rows={3} placeholder="Anote algo importante sobre este lançamento" /><span className="field-hint">{note.length}/1000 caracteres. Em lançamentos recorrentes, a observação acompanha as próximas ocorrências.</span></label>
      {isPj && type === 'Income' && <label className="checkbox-label"><input type="checkbox" checked={isMonthlyIncome} onChange={(event) => { setIsMonthlyIncome(event.target.checked); if (event.target.checked) setRecurrenceMode('none') }} />Este é meu recebimento PJ do mês</label>}
      {type === 'Expense' && <fieldset className="recurrence-options"><legend>Como registrar esta despesa?</legend><label><input type="radio" name="expense-recurrence" checked={recurrenceMode === 'none'} onChange={() => setRecurrenceMode('none')} />Uma vez</label><label><input type="radio" name="expense-recurrence" checked={recurrenceMode === 'monthly'} onChange={() => setRecurrenceMode('monthly')} />Mensal, sem data final</label><label><input type="radio" name="expense-recurrence" checked={recurrenceMode === 'installments'} onChange={() => setRecurrenceMode('installments')} />Mensal, com data final</label></fieldset>}
      {type === 'Income' && !isMonthlyIncome && <label className="checkbox-label"><input type="checkbox" checked={recurrenceMode === 'monthly'} onChange={(event) => setRecurrenceMode(event.target.checked ? 'monthly' : 'none')} />Repetir mensalmente</label>}
      {isInstallment && <div className="installment-fields"><label>Mês do último lançamento<input type="month" lang="pt-BR" value={installmentEndMonth} min={date.slice(0, 7)} onChange={(event) => setInstallmentEndMonth(event.target.value)} required /></label><label>Valor total do compromisso (opcional)<input inputMode="numeric" value={totalPurchaseAmount} onChange={(event) => setTotalPurchaseAmount(formatMoneyInput(event.target.value))} placeholder="0,00" /></label><p className="field-hint">Use para compras parceladas ou outras despesas com prazo definido. O total é apenas uma referência; o saldo considera cada lançamento mensal.</p>{installmentCount >= 2 && amount && <p className="installment-preview">{installmentCount} lançamentos de {currencyFormatter.format(parseMoney(amount))} · Total previsto: {currencyFormatter.format(installmentCount * parseMoney(amount))}</p>}</div>}
      {error && <div className="form-error" role="alert">{error}</div>}
      <div className="transaction-entry-form-actions"><button aria-busy={submitting} className={`transaction-submit ${type === 'Income' ? 'transaction-submit-income' : 'transaction-submit-expense'}`} disabled={submitting} type="submit">{submitting ? 'Salvando...' : `Salvar ${type === 'Income' ? 'receita' : 'despesa'}`}</button></div>
    </form>
  )
}
