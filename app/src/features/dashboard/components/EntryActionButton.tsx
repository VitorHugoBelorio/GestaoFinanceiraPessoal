import type { TransactionType } from '../../../types/api'

type EntryActionButtonProps = {
  type: TransactionType
  label: string
  onClick(): void
}

export function EntryActionButton({ type, label, onClick }: EntryActionButtonProps) {
  return (
    <button className={`button entry-action ${type === 'Income' ? 'entry-action-income' : 'entry-action-expense'}`} type="button" onClick={onClick}>
      <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"><path d="M12 5v14M5 12h14" /></svg>
      {label}
    </button>
  )
}
