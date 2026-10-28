import { useEffect, useState, type FormEvent } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingState } from '../components/ui/LoadingState'
import { CategoryPicker } from '../features/dashboard/components/CategoryPicker'
import { DashboardSidebar } from '../features/dashboard/components/DashboardSidebar'
import { currencyFormatter } from '../features/dashboard/dashboard.formatters'
import { readDashboardOrigin } from '../features/dashboard/dashboard.navigation'
import { useSidebarCollapsed } from '../features/dashboard/useSidebarCollapsed'
import type { Category, FinancialTransaction } from '../types/api'
import { formatMoneyInput, parseMoney } from '../utils/money'

type EditDraft = {
  description: string
  note: string
  amount: string
  date: string
  categoryId: number | null
  endMonth: string
  totalPurchaseAmount: string
}

function makeDraft(transaction: FinancialTransaction): EditDraft {
  return {
    description: transaction.description,
    note: transaction.note ?? '',
    amount: formatMoneyInput(String(Math.round(transaction.amount * 100))),
    date: transaction.date,
    categoryId: transaction.categoryId,
    endMonth: transaction.recurrenceEndDate?.slice(0, 7) ?? '',
    totalPurchaseAmount: transaction.totalPurchaseAmount === null ? '' : formatMoneyInput(String(Math.round(transaction.totalPurchaseAmount * 100))),
  }
}

function displayDate(value: string) {
  return new Date(`${value}T12:00:00`).toLocaleDateString('pt-BR')
}

function monthEnd(value: string) {
  const [year, month] = value.split('-').map(Number)
  return `${value}-${String(new Date(year, month, 0).getDate()).padStart(2, '0')}`
}

export function TransactionDetailPage() {
  const { id } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const [sidebarCollapsed, setSidebarCollapsed] = useSidebarCollapsed()
  const [transaction, setTransaction] = useState<FinancialTransaction | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [draft, setDraft] = useState<EditDraft | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [editing, setEditing] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState('')
  const origin = readDashboardOrigin(location.state)

  useEffect(() => { window.scrollTo(0, 0) }, [])

  useEffect(() => {
    let active = true
    if (!id || !/^\d+$/.test(id)) {
      setError('Não encontramos esse lançamento.')
      setLoading(false)
      return
    }
    Promise.all([apiRequest<FinancialTransaction>(`/transactions/${id}`), apiRequest<Category[]>('/categories')])
      .then(([entry, categoryData]) => {
        if (!active) return
        setTransaction(entry)
        setDraft(makeDraft(entry))
        setCategories(categoryData)
      })
      .catch((requestError: unknown) => {
        if (active) setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível abrir este lançamento.')
      })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id])

  function returnToDashboard(tab = origin?.tab ?? 'transactions') {
    const [year, month] = (transaction?.date ?? '').split('-').map(Number)
    const today = new Date()
    navigate('/app', {
      replace: true,
      state: { origin: origin
        ? { ...origin, tab }
        : { tab, year: year || today.getFullYear(), month: month || today.getMonth() + 1, categoryFilter: 'all' } },
    })
  }

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  async function handleSave(event: FormEvent) {
    event.preventDefault()
    if (!transaction || !draft || !id) return
    setError('')
    const amount = parseMoney(draft.amount)
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Informe um valor maior que zero.')
      return
    }
    if (transaction.isInstallment && !draft.endMonth) {
      setError('Informe o mês do último lançamento.')
      return
    }
    if (transaction.isRecurring && draft.endMonth && draft.endMonth < (transaction.recurrenceStartDate ?? transaction.date).slice(0, 7)) {
      setError('O mês final não pode ser anterior ao primeiro lançamento.')
      return
    }
    if (transaction.isInstallment && draft.endMonth) {
      const [startYear, startMonth] = (transaction.recurrenceStartDate ?? transaction.date).split('-').map(Number)
      const [endYear, endMonth] = draft.endMonth.split('-').map(Number)
      if ((endYear - startYear) * 12 + endMonth - startMonth < 1) {
        setError('A despesa com prazo definido precisa durar pelo menos dois meses.')
        return
      }
    }
    if (transaction.isInstallment && draft.totalPurchaseAmount && parseMoney(draft.totalPurchaseAmount) <= 0) {
      setError('O valor total informado precisa ser maior que zero.')
      return
    }

    setSaving(true)
    try {
      await apiRequest<void>(`/transactions/${id}`, {
        method: 'PUT',
        body: JSON.stringify({
          description: draft.description,
          note: draft.note.trim() || null,
          amount,
          date: draft.date,
          categoryId: draft.categoryId,
          recurrenceEndDate: transaction.isRecurring && draft.endMonth ? monthEnd(draft.endMonth) : null,
          totalPurchaseAmount: transaction.isInstallment && draft.totalPurchaseAmount ? parseMoney(draft.totalPurchaseAmount) : null,
        }),
      })
      returnToDashboard()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível salvar as alterações. Tente novamente.')
    } finally { setSaving(false) }
  }

  async function handleDelete() {
    if (!id) return
    setDeleting(true)
    setError('')
    try {
      await apiRequest<void>(`/transactions/${id}`, { method: 'DELETE' })
      returnToDashboard()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível excluir o lançamento. Tente novamente.')
      setDeleting(false)
    }
  }

  const isIncome = transaction?.type === 'Income'
  return (
    <div className={`app-shell${sidebarCollapsed ? ' is-sidebar-collapsed' : ''}`}>
      <DashboardSidebar
        activeTab={origin?.tab ?? 'transactions'}
        userName={user?.name ?? ''}
        userEmail={user?.email ?? ''}
        collapsed={sidebarCollapsed}
        navigationOnly
        onToggleCollapse={() => setSidebarCollapsed((current) => !current)}
        onLogout={() => { void handleLogout() }}
        onTabChange={returnToDashboard}
      />
      <main className="dashboard-content transaction-entry-page transaction-detail-page">
        <button className="back-link" type="button" onClick={() => returnToDashboard()}>← Voltar aos lançamentos</button>
        {loading && <LoadingState label="Abrindo lançamento..." />}
        {error && !transaction && <div className="form-error" role="alert">{error}</div>}
        {transaction && draft && <>
          <header className="transaction-entry-heading transaction-detail-heading">
            <span className={`eyebrow ${isIncome ? 'entry-tone-income' : 'entry-tone-expense'}`}>{isIncome ? 'Receita' : 'Despesa'} {transaction.isRecurring ? '· Série mensal' : '· Lançamento único'}</span>
            <h1>{transaction.description}</h1>
            <p className={`transaction-detail-amount ${isIncome ? 'positive' : 'negative'}`}>{currencyFormatter.format(transaction.amount)}</p>
          </header>

          {!editing && <section className="transaction-detail-card" aria-label="Detalhes do lançamento">
            <dl className="transaction-detail-grid">
              <div><dt>Data</dt><dd>{displayDate(transaction.date)}</dd></div>
              <div><dt>Tipo</dt><dd>{isIncome ? 'Receita' : 'Despesa'}</dd></div>
              <div><dt>Etiqueta</dt><dd>{transaction.categoryName ?? 'Sem etiqueta'}</dd></div>
              <div><dt>Registro</dt><dd>{transaction.status === 'Planned' ? 'Previsto' : 'Confirmado'}</dd></div>
              {transaction.isMonthlyIncome && <div><dt>Recebimento PJ</dt><dd>Registrado para este mês</dd></div>}
              {transaction.isRecurring && <>
                <div><dt>Primeiro lançamento</dt><dd>{displayDate(transaction.recurrenceStartDate ?? transaction.date)}</dd></div>
                <div><dt>Repetição</dt><dd>Mensal{transaction.recurrenceEndDate ? ` até ${transaction.recurrenceEndDate.slice(0, 7).split('-').reverse().join('/')}` : ', sem data final'}</dd></div>
              </>}
              {transaction.isInstallment && <>
                <div><dt>Ocorrência</dt><dd>{transaction.installmentNumber} de {transaction.installmentCount}</dd></div>
                {transaction.totalPurchaseAmount !== null && <div><dt>Valor total informado</dt><dd>{currencyFormatter.format(transaction.totalPurchaseAmount)}</dd></div>}
              </>}
              <div className="transaction-detail-note"><dt>Observação</dt><dd>{transaction.note || 'Nenhuma observação registrada.'}</dd></div>
            </dl>
            <div className="transaction-detail-actions">
              <button className="transaction-detail-edit" type="button" onClick={() => { setConfirmDelete(false); setError(''); setEditing(true) }}>Editar lançamento</button>
              <button className="transaction-detail-delete" type="button" onClick={() => { setEditing(false); setError(''); setConfirmDelete(true) }}>Excluir {transaction.isRecurring ? 'série' : 'lançamento'}</button>
            </div>
          </section>}

          {editing && <form className="transaction-form transaction-entry-form transaction-edit-form" onSubmit={(event) => { void handleSave(event) }}>
            <div className="transaction-detail-section-title"><h2>Editar {transaction.isRecurring ? 'série mensal' : 'lançamento'}</h2><p>{transaction.isRecurring ? 'As alterações de descrição, valor, etiqueta e observação valem para todos os meses da série, inclusive os já registrados.' : 'Corrija os dados deste lançamento.'}</p></div>
            <label className="transaction-description">Descrição<input value={draft.description} minLength={2} maxLength={180} onChange={(event) => setDraft({ ...draft, description: event.target.value })} required /></label>
            <label>Valor<input inputMode="numeric" value={draft.amount} onChange={(event) => setDraft({ ...draft, amount: formatMoneyInput(event.target.value) })} required /></label>
            <label>Data<input type="date" lang="pt-BR" value={draft.date} disabled={transaction.isRecurring} onChange={(event) => setDraft({ ...draft, date: event.target.value })} required /></label>
            {transaction.isRecurring && <p className="field-hint transaction-detail-full">A data desta ocorrência fica fixa para manter a sequência mensal.</p>}
            <CategoryPicker type={transaction.type} categories={categories} value={draft.categoryId} onChange={(categoryId) => setDraft({ ...draft, categoryId })} onCreated={(category) => setCategories((current) => [...current, category].sort((left, right) => left.name.localeCompare(right.name, 'pt-BR')))} />
            <label className="transaction-note-field">Observação (opcional)<textarea value={draft.note} maxLength={1000} rows={4} onChange={(event) => setDraft({ ...draft, note: event.target.value })} /><span className="field-hint">{draft.note.length}/1000 caracteres</span></label>
            {transaction.isRecurring && <div className="installment-fields"><label>Mês do último lançamento {transaction.isInstallment ? '' : '(opcional)'}<input type="month" lang="pt-BR" value={draft.endMonth} min={(transaction.recurrenceStartDate ?? transaction.date).slice(0, 7)} required={transaction.isInstallment} onChange={(event) => setDraft({ ...draft, endMonth: event.target.value })} /></label>{transaction.isInstallment && <label>Valor total do compromisso (opcional)<input inputMode="numeric" value={draft.totalPurchaseAmount} onChange={(event) => setDraft({ ...draft, totalPurchaseAmount: formatMoneyInput(event.target.value) })} placeholder="0,00" /></label>}<p className="field-hint">Ao encurtar o período, os lançamentos posteriores ao novo mês final são removidos. Ao ampliar, os próximos meses serão gerados conforme a nova data.</p></div>}
            {error && <div className="form-error" role="alert">{error}</div>}
            <div className="transaction-detail-actions transaction-detail-full"><button className="transaction-detail-cancel" type="button" disabled={saving} onClick={() => { setDraft(makeDraft(transaction)); setEditing(false); setError('') }}>Cancelar edição</button><button className={`transaction-submit ${isIncome ? 'transaction-submit-income' : 'transaction-submit-expense'}`} type="submit" disabled={saving} aria-busy={saving}>{saving ? 'Salvando...' : 'Salvar alterações'}</button></div>
          </form>}

          {confirmDelete && <section className="transaction-delete-confirm" aria-labelledby="delete-transaction-title"><h2 id="delete-transaction-title">Excluir {transaction.isRecurring ? 'toda a série' : 'este lançamento'}?</h2><p>{transaction.isRecurring ? 'Todos os meses desta série, inclusive os já registrados, serão removidos. Nenhum novo lançamento será gerado. Esta ação não pode ser desfeita.' : 'Este lançamento será removido do seu histórico e do saldo. Esta ação não pode ser desfeita.'}</p>{error && <div className="form-error" role="alert">{error}</div>}<div className="transaction-detail-actions"><button className="transaction-detail-cancel" type="button" disabled={deleting} onClick={() => { setConfirmDelete(false); setError('') }}>Manter lançamento</button><button className="transaction-detail-delete-confirm" type="button" disabled={deleting} aria-busy={deleting} onClick={() => { void handleDelete() }}>{deleting ? 'Excluindo...' : transaction.isRecurring ? 'Excluir série inteira' : 'Excluir lançamento'}</button></div></section>}
        </>}
      </main>
    </div>
  )
}
