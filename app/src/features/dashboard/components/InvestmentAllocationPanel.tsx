import { useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import { CurrencyInput } from '../../../components/ui/CurrencyInput'
import type { InvestmentAllocation } from '../../../types/api'
import { parseMoney } from '../../../utils/money'
import { currencyFormatter } from '../dashboard.formatters'

type Props = {
  allocations: InvestmentAllocation[]
  investedBalance: number
  onUpdated(): void
  onSetOpening(): void
  onAddInvestment(): void
}

const inputMoneyFormatter = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function InvestmentAllocationPanel({ allocations, investedBalance, onUpdated, onSetOpening, onAddInvestment }: Props) {
  const [editingId, setEditingId] = useState<number | null>(null)
  const [name, setName] = useState('')
  const [amountText, setAmountText] = useState('')
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const classified = Math.round(allocations.reduce((total, item) => total + item.amount, 0) * 100) / 100
  const unclassified = Math.max(0, Math.round((investedBalance - classified) * 100) / 100)
  const current = allocations.find((item) => item.id === editingId)

  function startNew() {
    setEditingId(0)
    setName('')
    setAmountText('')
    setConfirmDeleteId(null)
    setError('')
  }

  function startEdit(allocation: InvestmentAllocation) {
    setEditingId(allocation.id)
    setName(allocation.name)
    setAmountText(inputMoneyFormatter.format(allocation.amount))
    setConfirmDeleteId(null)
    setError('')
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    const trimmedName = name.trim()
    const amount = parseMoney(amountText)
    if (!trimmedName || trimmedName.length > 80) {
      setError('Dê um nome de até 80 caracteres para esse grupo.')
      return
    }
    if (allocations.some((item) => item.id !== editingId && item.name.toLocaleLowerCase('pt-BR') === trimmedName.toLocaleLowerCase('pt-BR'))) {
      setError('Você já tem um grupo com esse nome.')
      return
    }
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Informe um valor maior que zero.')
      return
    }
    if (amount > unclassified + (current?.amount ?? 0)) {
      setError(`Você pode distribuir até ${currencyFormatter.format(unclassified + (current?.amount ?? 0))} sem ultrapassar o valor aplicado.`)
      return
    }

    setBusy(true)
    setError('')
    try {
      await apiRequest(`/investments/allocations${editingId ? `/${editingId}` : ''}`, {
        method: editingId ? 'PUT' : 'POST',
        body: JSON.stringify({ name: trimmedName, amount }),
      })
      setEditingId(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError && requestError.status === 400
        ? 'Confira o nome e o valor. A soma dos grupos não pode superar o valor aplicado.'
        : requestError instanceof ApiError ? requestError.message : 'Não foi possível salvar esse grupo. Tente novamente.')
    } finally { setBusy(false) }
  }

  async function remove(id: number) {
    setBusy(true)
    setError('')
    try {
      await apiRequest<void>(`/investments/allocations/${id}`, { method: 'DELETE' })
      setConfirmDeleteId(null)
      onUpdated()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível excluir esse grupo.')
    } finally { setBusy(false) }
  }

  return (
    <section className="investment-groups" aria-labelledby="investment-groups-title">
      <div className="investment-groups-heading">
        <div><span className="capital-plan-kicker">Sua carteira</span><h2 id="investment-groups-title">Como seu dinheiro está investido</h2></div>
        <p>Separe o valor aplicado atual em grupos com nomes escolhidos por você, como CDBs, fundos imobiliários ou imóveis. Essa divisão não depende do mês selecionado, não cria um aporte e não usa cotações de mercado.</p>
      </div>
      {investedBalance <= 0 ? <div className="investment-groups-first-step">
        <p>Você ainda não informou nenhum valor investido. Antes de separar por grupos, escolha de onde veio o dinheiro:</p>
        <div><button type="button" onClick={onSetOpening}>Já estava investido no início</button><button type="button" onClick={onAddInvestment}>Investi depois do cadastro</button></div>
      </div> : <>
      <div className="investment-groups-summary" aria-label="Distribuição do valor aplicado">
        <span>Em grupos <strong>{currencyFormatter.format(classified)}</strong></span>
        <span>Sem classificar <strong>{currencyFormatter.format(unclassified)}</strong></span>
      </div>
      <p className="investment-groups-caveat">Inclua imóveis aqui somente se o valor deles já fizer parte do capital aplicado registrado. Bens fora desse saldo precisarão de um controle patrimonial próprio para não distorcer seu saldo financeiro.</p>
      {allocations.length > 0 && <ul className="investment-groups-list">{allocations.map((allocation) => (
        <li key={allocation.id}>
          <span className="investment-groups-name">{allocation.name}</span>
          <strong>{currencyFormatter.format(allocation.amount)}</strong>
          {confirmDeleteId === allocation.id ? <div className="investment-groups-confirm"><small>O valor continuará aplicado, mas sem grupo.</small><button type="button" disabled={busy} onClick={() => { void remove(allocation.id) }}>Excluir grupo</button><button type="button" disabled={busy} onClick={() => setConfirmDeleteId(null)}>Manter</button></div> : <div className="investment-groups-row-actions"><button type="button" disabled={busy} onClick={() => startEdit(allocation)}>Editar</button><button type="button" disabled={busy} onClick={() => { setConfirmDeleteId(allocation.id); setEditingId(null); setError('') }}>Excluir</button></div>}
        </li>
      ))}</ul>}
      {allocations.length === 0 && <p className="investment-groups-empty">Nenhum grupo criado. O valor aplicado aparece como “sem classificar” até você distribuí-lo.</p>}
      {editingId === null ? <button className="investment-groups-add" type="button" onClick={startNew}>+ Adicionar grupo</button> : <form className="investment-groups-form" onSubmit={(event) => { void save(event) }}>
        <label>Nome do grupo<input autoFocus maxLength={80} list="investment-group-suggestions" value={name} onChange={(event) => setName(event.target.value)} placeholder="Ex.: CDB, fundos imobiliários, imóvel" required /></label>
        <datalist id="investment-group-suggestions"><option value="CDB e renda fixa" /><option value="Fundos imobiliários" /><option value="Imóveis" /><option value="Ações" /><option value="Tesouro Direto" /></datalist>
        <label>Valor investido nesse grupo<CurrencyInput value={amountText} onValueChange={setAmountText} /></label>
        <div className="investment-groups-form-actions"><button type="button" disabled={busy} onClick={() => { setEditingId(null); setError('') }}>Cancelar</button><button type="submit" disabled={busy}>{busy ? 'Salvando...' : editingId ? 'Salvar grupo' : 'Adicionar grupo'}</button></div>
      </form>}
      </>}
      {error && <div className="form-error" role="alert">{error}</div>}
    </section>
  )
}
