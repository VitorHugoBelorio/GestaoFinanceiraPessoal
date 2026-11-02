import * as m from 'motion/react-m'
import { fadeUpItem, staggerContainer } from '../../../motion/variants'
import type { DashboardData } from '../../../types/api'
import { currencyFormatter } from '../dashboard.formatters'

export function SummaryCards({ dashboard }: { dashboard: DashboardData }) {
  return (
    <m.section className="summary-grid" variants={staggerContainer} initial="hidden" animate="visible">
      <m.article variants={fadeUpItem} className="summary-card summary-card-featured">
        <span>Saldo total</span><strong>{currencyFormatter.format(dashboard.totalBalance)}</strong><small>Capital acumulado</small>
      </m.article>
      <m.article variants={fadeUpItem} className="summary-card summary-card-featured">
        <span>Recebimento do mês</span><strong className="positive">{currencyFormatter.format(dashboard.monthlyPrimaryIncome)}</strong><small>{dashboard.monthlyPrimaryIncome > 0 ? 'Salário ou recebimento principal confirmado' : 'Identifique o salário ou recebimento ao registrar a receita'}</small>
      </m.article>
      <m.article variants={fadeUpItem} className="summary-card">
        <span>Receitas do mês</span><strong className="positive">{currencyFormatter.format(dashboard.monthlyIncome)}</strong><small>{dashboard.monthlyPrimaryIncome > 0 ? 'Recebimento principal + outras entradas' : 'Todas as entradas confirmadas'}</small>
      </m.article>
      <m.article variants={fadeUpItem} className="summary-card">
        <span>Despesas do mês</span><strong className="negative">{currencyFormatter.format(dashboard.monthlyExpense)}</strong><small>Saídas confirmadas</small>
      </m.article>
    </m.section>
  )
}
