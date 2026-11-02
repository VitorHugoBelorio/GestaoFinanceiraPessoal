import { useEffect, useState } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import { LoadingState } from '../../../components/ui/LoadingState'
import type { FinancialTransaction } from '../../../types/api'
import { isTransactionList } from '../../../utils/financialData'
import { annualCashflow, expenseByCategory, monthlyCashflow, type AnnualBucket, type CashflowBucket } from '../charts.data'
import { currencyFormatter } from '../dashboard.formatters'

export type ChartsMode = 'monthly' | 'annual'

type Props = {
  mode: ChartsMode
  onModeChange(mode: ChartsMode): void
  year: number
  month: number
  monthlyAvailable: boolean
  monthlyTransactions: FinancialTransaction[]
  onSelectMonth(month: number): void
  onPreviousMonth(): void
}

const axisFormatter = new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 })

function localDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function CashflowChart({ buckets, label }: { buckets: CashflowBucket[]; label: string }) {
  const width = 720
  const left = 55
  const right = 16
  const top = 20
  const baseline = 190
  const height = baseline - top
  const step = (width - left - right) / buckets.length
  const barWidth = Math.min(23, step * .27)
  const max = Math.max(1, ...buckets.flatMap((bucket) => [bucket.income, bucket.expense]))
  const hasData = buckets.some((bucket) => bucket.income > 0 || bucket.expense > 0)

  return <div className="analytics-chart-scroll"><svg className="analytics-svg" viewBox="0 0 720 230" role="img" aria-label={label}>
    {[0, .5, 1].map((fraction) => {
      const y = baseline - fraction * height
      return <g key={fraction}><line x1={left} y1={y} x2={width - right} y2={y} className="analytics-grid-line" />{(hasData || fraction === 0) && <text x={left - 8} y={y + 4} textAnchor="end" className="analytics-axis-label">{axisFormatter.format(max * fraction)}</text>}</g>
    })}
    {buckets.map((bucket, index) => {
      const center = left + (index + .5) * step
      const incomeHeight = bucket.income / max * height
      const expenseHeight = bucket.expense / max * height
      return <g key={bucket.label}>
        <rect x={center - barWidth - 2} y={baseline - incomeHeight} width={barWidth} height={incomeHeight} rx="3" className="analytics-bar-income"><title>{bucket.label}: receitas {currencyFormatter.format(bucket.income)}</title></rect>
        <rect x={center + 2} y={baseline - expenseHeight} width={barWidth} height={expenseHeight} rx="3" className="analytics-bar-expense"><title>{bucket.label}: despesas {currencyFormatter.format(bucket.expense)}</title></rect>
        <text x={center} y={213} textAnchor="middle" className="analytics-axis-label">{bucket.label}</text>
      </g>
    })}
    {!hasData && <text x="380" y="108" textAnchor="middle" className="analytics-empty-label">Sem lançamentos confirmados</text>}
  </svg></div>
}

function AnnualTrendChart({ buckets }: { buckets: AnnualBucket[] }) {
  const actual = buckets.filter((bucket) => !bucket.isFuture)
  const hasData = actual.some((bucket) => bucket.count > 0)
  const values = actual.map((bucket) => bucket.cumulativeNet)
  const minimum = Math.min(0, ...values)
  const maximum = Math.max(0, ...values)
  const span = Math.max(1, maximum - minimum)
  const left = 55
  const baseline = 155
  const top = 16
  const step = (720 - left - 16) / 12
  const point = (bucket: AnnualBucket) => ({
    x: left + (bucket.month - .5) * step,
    y: baseline - (bucket.cumulativeNet - minimum) / span * (baseline - top),
  })
  const path = actual.map((bucket, index) => `${index ? 'L' : 'M'} ${point(bucket).x} ${point(bucket).y}`).join(' ')
  const zeroY = baseline - (0 - minimum) / span * (baseline - top)

  return <div className="analytics-chart-scroll"><svg className="analytics-svg" viewBox="0 0 720 195" role="img" aria-label="Evolução do resultado acumulado de receitas menos despesas ao longo do ano">
    <line x1={left} y1={zeroY} x2="704" y2={zeroY} className="analytics-zero-line" />
    <text x={left - 8} y={zeroY + 4} textAnchor="end" className="analytics-axis-label">0</text>
    {hasData && path && <path d={path} className="analytics-trend-line" />}
    {hasData && actual.map((bucket) => <g key={bucket.month}>
      <circle cx={point(bucket).x} cy={point(bucket).y} r="4.5" className="analytics-trend-point"><title>{bucket.label}: resultado acumulado {currencyFormatter.format(bucket.cumulativeNet)}</title></circle>
    </g>)}
    {!hasData && <text x="380" y="91" textAnchor="middle" className="analytics-empty-label">Sem evolução registrada</text>}
    {buckets.map((bucket) => <text key={bucket.month} x={left + (bucket.month - .5) * step} y="182" textAnchor="middle" className={`analytics-axis-label${bucket.isFuture ? ' is-future' : ''}`}>{bucket.label}</text>)}
  </svg></div>
}

function ExpenseCategoryChart({ categories }: { categories: ReturnType<typeof expenseByCategory> }) {
  const total = categories.reduce((sum, category) => sum + category.amount, 0)
  const circumference = 2 * Math.PI * 69
  const colors = ['var(--negative)', 'var(--accent)', 'var(--text-soft)', 'var(--accent-text)', 'var(--muted)']
  let offset = 0

  return <div className="analytics-category-chart">
    <svg viewBox="0 0 240 200" role="img" aria-label={total > 0 ? 'Distribuição das despesas por etiqueta' : 'Nenhuma despesa confirmada neste mês'}>
      <circle cx="120" cy="98" r="69" fill="none" stroke="var(--surface-subtle)" strokeWidth="19" />
      {categories.map((category, index) => {
        const length = category.amount / total * circumference
        const dashOffset = offset
        offset += length
        return <circle key={category.name} cx="120" cy="98" r="69" fill="none" stroke={colors[index]} strokeWidth="19" strokeDasharray={`${length} ${circumference - length}`} strokeDashoffset={-dashOffset} transform="rotate(-90 120 98)"><title>{category.name}: {currencyFormatter.format(category.amount)}</title></circle>
      })}
      <text x="120" y="91" textAnchor="middle" className="analytics-donut-caption">{total > 0 ? 'Despesas' : 'Sem despesas'}</text>
      {total > 0 && <text x="120" y="115" textAnchor="middle" className="analytics-donut-total">R$ {axisFormatter.format(total)}</text>}
    </svg>
    {categories.length > 0 && <ul className="analytics-category-legend">{categories.map((category, index) => <li key={category.name}><span className="analytics-category-dot" style={{ backgroundColor: colors[index] }} /><span>{category.name}</span><strong>{Math.round(category.amount / total * 100)}%</strong></li>)}</ul>}
  </div>
}

export function ChartsPanel({ mode, onModeChange, year, month, monthlyAvailable, monthlyTransactions, onSelectMonth, onPreviousMonth }: Props) {
  const [annualResult, setAnnualResult] = useState<{ year: number; entries: FinancialTransaction[] } | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)

  useEffect(() => {
    if (mode !== 'annual') return
    let active = true
    const today = new Date()
    setError('')
    if (year > today.getFullYear()) {
      setAnnualResult({ year, entries: [] })
      setLoading(false)
      return
    }

    setLoading(true)
    const end = year === today.getFullYear() ? localDate(today) : `${year}-12-31`
    apiRequest<unknown>(`/transactions?from=${year}-01-01&to=${end}`)
      .then((entries) => {
        if (!active) return
        if (!isTransactionList(entries)) throw new Error('Dados financeiros incompletos.')
        setAnnualResult({ year, entries })
      })
      .catch((requestError: unknown) => {
        if (active) setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível carregar a análise anual.')
      })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [mode, year, retry])

  const monthBuckets = monthlyCashflow(monthlyTransactions, year, month)
  const monthCategories = expenseByCategory(monthlyTransactions, year, month)
  const monthConfirmed = monthlyTransactions.filter((transaction) => transaction.status === 'Confirmed').length
  const monthPlanned = monthlyTransactions.filter((transaction) => transaction.status === 'Planned').length
  const annualEntries = annualResult?.year === year ? annualResult.entries : null
  const annualBuckets = annualEntries ? annualCashflow(annualEntries, year) : []
  const annualCount = annualBuckets.reduce((total, bucket) => total + bucket.count, 0)
  const annualPlanned = annualEntries?.filter((transaction) => transaction.status === 'Planned').length ?? 0
  const monthLabel = new Date(year, month - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })

  return <div className="analytics-page">
    <div className="analytics-heading">
      <div><span className="eyebrow">Gráficos e análises</span><h2>Entenda seus números</h2><p>Compare o dinheiro que entrou e saiu. A visão mensal acompanha o mês selecionado; a anual mostra a evolução de janeiro a dezembro.</p></div>
      <div className="analytics-mode" role="group" aria-label="Período dos gráficos"><button type="button" aria-pressed={mode === 'monthly'} className={mode === 'monthly' ? 'active' : ''} onClick={() => onModeChange('monthly')}>Mensal</button><button type="button" aria-pressed={mode === 'annual'} className={mode === 'annual' ? 'active' : ''} onClick={() => onModeChange('annual')}>Anual</button></div>
    </div>

    {mode === 'monthly' ? monthlyAvailable && <>
      <div className="analytics-layout">
        <section className="analytics-card"><div className="analytics-card-heading"><div><h3>Entradas e saídas por semana</h3><p>Distribuição dos lançamentos confirmados em {monthLabel}.</p></div><div className="analytics-legend"><span className="income">Receitas</span><span className="expense">Despesas</span></div></div><CashflowChart buckets={monthBuckets} label={`Receitas e despesas por semana em ${monthLabel}`} /></section>
        <section className="analytics-card analytics-categories"><div className="analytics-card-heading"><div><h3>Despesas por etiqueta</h3><p>Veja como seus gastos se distribuem.</p></div></div><ExpenseCategoryChart categories={monthCategories} /></section>
      </div>
      {monthConfirmed === 0 && <div className="analytics-inline-hint" role="status"><span>Sem lançamentos confirmados em {monthLabel}. Os gráficos aparecem sem barras até haver registros.</span><button type="button" onClick={onPreviousMonth}>Ver mês anterior</button></div>}
      {monthPlanned > 0 && <p className="analytics-note">{monthPlanned} lançamento{monthPlanned === 1 ? ' previsto não entra' : 's previstos não entram'} nos totais e gráficos até a confirmação.</p>}
    </> : <>
      {loading && !annualEntries && <div className="analytics-loading"><LoadingState label="Preparando análise anual..." /></div>}
      {error && <div className="form-error" role="alert">{error} <button className="capital-retry" type="button" onClick={() => setRetry((current) => current + 1)}>Tentar novamente</button></div>}
      {annualEntries && !error && <div className={loading ? 'analytics-results is-updating' : 'analytics-results'} aria-busy={loading} inert={loading}>
        <div className="analytics-annual-grid">
          <section className="analytics-card"><div className="analytics-card-heading"><div><h3>Entradas e saídas mês a mês</h3><p>Compare os valores confirmados em cada mês.</p></div><div className="analytics-legend"><span className="income">Receitas</span><span className="expense">Despesas</span></div></div><CashflowChart buckets={annualBuckets} label={`Receitas e despesas mensais em ${year}`} /></section>
          <section className="analytics-card"><div className="analytics-card-heading"><div><h3>Resultado acumulado no ano</h3><p>Somamos a diferença entre receitas e despesas a cada mês.</p></div></div><AnnualTrendChart buckets={annualBuckets} /></section>
        </div>
        {annualCount === 0 && <p className="analytics-inline-hint" role="status">Sem lançamentos confirmados em {year}. Os gráficos permanecem visíveis para você comparar ao escolher outro ano.</p>}
        <p className="analytics-note">Meses que ainda não ocorreram ficam sem valores. O resultado acumulado mostra a soma das receitas menos despesas do ano, não o saldo total da conta.</p>
        {annualCount > 0 && <details className="analytics-details"><summary>Consultar valores mês a mês</summary><section className="analytics-card analytics-year-table"><div className="analytics-card-heading"><div><h3>Valores do ano</h3><p>Abra um mês para ver a análise detalhada.</p></div></div><div className="table-scroll"><table><thead><tr><th>Mês</th><th>Receitas</th><th>Despesas</th><th>Resultado</th><th>Acumulado no ano</th><th>Abrir</th></tr></thead><tbody>{annualBuckets.map((bucket) => <tr key={bucket.month}><td>{new Date(year, bucket.month - 1, 1).toLocaleDateString('pt-BR', { month: 'long' })}</td><td>{bucket.isFuture ? '—' : currencyFormatter.format(bucket.income)}</td><td>{bucket.isFuture ? '—' : currencyFormatter.format(bucket.expense)}</td><td className={!bucket.isFuture && bucket.income - bucket.expense < 0 ? 'negative' : undefined}>{bucket.isFuture ? 'Ainda não ocorreu' : currencyFormatter.format(bucket.income - bucket.expense)}</td><td className={!bucket.isFuture && bucket.cumulativeNet < 0 ? 'negative' : undefined}>{bucket.isFuture ? '—' : currencyFormatter.format(bucket.cumulativeNet)}</td><td>{!bucket.isFuture && <button type="button" onClick={() => onSelectMonth(bucket.month)}>Ver mês</button>}</td></tr>)}</tbody></table></div></section></details>}
        {annualPlanned > 0 && <p className="analytics-note">Lançamentos previstos não entram nos gráficos até a confirmação.</p>}
      </div>}
    </>}
    <p className="analytics-note">O resultado mostra somente receitas e despesas confirmadas. Capital informado no cadastro, aportes e resgates entre disponível e investido não são novas receitas nem despesas; rendimentos de mercado ainda não são calculados automaticamente.</p>
  </div>
}
