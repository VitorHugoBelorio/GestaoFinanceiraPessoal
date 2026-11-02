import { monthFormatter } from '../dashboard.formatters'
import { DashboardIcon } from './DashboardIcon'

type DashboardHeaderProps = {
  referenceDate: Date
  updating: boolean
  userName: string
  periodMode?: 'month' | 'year'
  onNextPeriod(): void
  onPreviousPeriod(): void
}

export function DashboardHeader({ referenceDate, updating, userName, periodMode = 'month', onNextPeriod, onPreviousPeriod }: DashboardHeaderProps) {
  const isYear = periodMode === 'year'
  return (
    <header className="dashboard-header">
      <div><p className="dashboard-greeting">Olá, <strong>{userName}</strong></p><h1>Seu panorama financeiro</h1></div>
      <div className="month-picker" role="group" aria-label={isYear ? 'Navegação entre anos' : 'Navegação entre meses'}>
        <button type="button" aria-label={isYear ? 'Ano anterior' : 'Mês anterior'} onClick={onPreviousPeriod}><DashboardIcon name="chevron-left" /></button>
        <strong aria-live="polite">{isYear ? referenceDate.getFullYear() : monthFormatter.format(referenceDate)}</strong>
        <span className="month-picker-status" role="status" aria-live="polite">{updating ? `Atualizando ${isYear ? 'ano' : 'mês'}...` : ''}</span>
        <button type="button" aria-label={isYear ? 'Próximo ano' : 'Próximo mês'} onClick={onNextPeriod}><DashboardIcon name="chevron-right" /></button>
      </div>
    </header>
  )
}
