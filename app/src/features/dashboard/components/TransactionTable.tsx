import * as m from 'motion/react-m'
import { fadeUpItem, staggerContainer } from '../../../motion/variants'
import type { FinancialTransaction } from '../../../types/api'
import { currencyFormatter } from '../dashboard.formatters'
import { EntryActionButton } from './EntryActionButton'

type TransactionTableProps = {
  title?: string
  transactions: FinancialTransaction[]
  emptyMessage?: string
  onAddIncome?: () => void
  onAddExpense?: () => void
  onViewTransaction?: (transaction: FinancialTransaction) => void
}

function TransactionNote({ note }: { note: string | null }) {
  if (!note) return <span className="category-empty">—</span>
  if (note.length <= 80) return <span className="transaction-note-text">{note}</span>

  return <span className="transaction-note-text">{note.slice(0, 80).trimEnd()}…</span>
}

export function TransactionTable({ title, transactions, emptyMessage = 'Nenhum lançamento neste período.', onAddIncome, onAddExpense, onViewTransaction }: TransactionTableProps) {
  return (
    <section className="table-card">
      {title && <div className="table-card-header"><h2>{title}</h2>{(onAddIncome || onAddExpense) && <div className="entry-actions">{onAddIncome && <EntryActionButton type="Income" label="Nova receita" onClick={onAddIncome} />}{onAddExpense && <EntryActionButton type="Expense" label="Nova despesa" onClick={onAddExpense} />}</div>}</div>}
      {transactions.length === 0 ? <div className="empty-table">{emptyMessage}</div> : (
        <div className="table-scroll">
          <table>
            <thead><tr><th>Data</th><th>Descrição</th><th>Tipo</th><th>Etiqueta</th><th>Observação</th><th>Valor</th></tr></thead>
            <m.tbody variants={staggerContainer} initial="hidden" animate="visible">
              {transactions.map((transaction) => (
                <m.tr variants={fadeUpItem} key={transaction.id} className={onViewTransaction ? 'transaction-clickable-row' : undefined} onClick={() => onViewTransaction?.(transaction)}>
                  <td>{new Date(`${transaction.date}T12:00:00`).toLocaleDateString('pt-BR')}</td>
                  <td>{onViewTransaction ? <button type="button" className="transaction-row-link" aria-label={`Ver detalhes de ${transaction.description}`} onClick={(event) => { event.stopPropagation(); onViewTransaction(transaction) }}>{transaction.description}<span aria-hidden="true">↗</span></button> : transaction.description}{transaction.isInstallment ? <small>Lançamento {transaction.installmentNumber}/{transaction.installmentCount}</small> : transaction.isRecurring && <small>Mensal</small>}{transaction.isInstallment && transaction.totalPurchaseAmount !== null && <small>Total informado: {currencyFormatter.format(transaction.totalPurchaseAmount)}</small>}</td>
                  <td><span className={`type-badge ${transaction.type === 'Income' ? 'income' : 'expense'}`}>{transaction.type === 'Income' ? 'Receita' : 'Despesa'}</span></td>
                  <td>{transaction.categoryName ? <span className="category-badge">{transaction.categoryName}</span> : <span className="category-empty">—</span>}</td>
                  <td className="transaction-note-cell"><TransactionNote note={transaction.note} /></td>
                  <td className={transaction.type === 'Income' ? 'positive' : 'negative'}>{currencyFormatter.format(transaction.amount)}</td>
                </m.tr>
              ))}
            </m.tbody>
          </table>
        </div>
      )}
    </section>
  )
}
