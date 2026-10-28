import { monthFormatter } from '../dashboard.formatters'
import { DashboardIcon } from './DashboardIcon'

type DashboardHeaderProps = {
  referenceDate: Date
  updating: boolean
  userName: string
  onNextMonth(): void
  onPreviousMonth(): void
}

export function DashboardHeader({ referenceDate, updating, userName, onNextMonth, onPreviousMonth }: DashboardHeaderProps) {
  return (
    <header className="dashboard-header">
      <div><p className="dashboard-greeting">Olá, <strong>{userName}</strong></p><h1>Seu panorama financeiro</h1></div>
      <div className="month-picker" role="group" aria-label="Navegação entre meses">
        <button type="button" aria-label="Mês anterior" onClick={onPreviousMonth}><DashboardIcon name="chevron-left" /></button>
        <strong aria-live="polite">{monthFormatter.format(referenceDate)}</strong>
        <span className="month-picker-status" role="status" aria-live="polite">{updating ? 'Atualizando mês...' : ''}</span>
        <button type="button" aria-label="Próximo mês" onClick={onNextMonth}><DashboardIcon name="chevron-right" /></button>
      </div>
    </header>
  )
}
