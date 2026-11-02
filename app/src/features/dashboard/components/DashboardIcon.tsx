import type { ReactNode } from 'react'

type DashboardIconName = 'overview' | 'transactions' | 'periods' | 'planning' | 'investments' | 'charts' | 'collapse' | 'expand' | 'logout' | 'chevron-left' | 'chevron-right'

export function DashboardIcon({ name }: { name: DashboardIconName }) {
  const paths: Record<DashboardIconName, ReactNode> = {
    overview: <><rect x="3" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="3" width="7" height="7" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" /><rect x="14" y="14" width="7" height="7" rx="1.5" /></>,
    transactions: <><path d="M4 7h15m-4-3 4 3-4 3" /><path d="M20 17H5m4-3-4 3 4 3" /></>,
    investments: <><path d="M3 17.5 9 12l4 3 8-8" /><path d="M16 7h5v5" /><path d="M3 21h18" /></>,
    periods: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M7 3v4m10-4v4M3 10h18m-13 4h3m-3 3h6" /></>,
    planning: <><path d="M4 20V7a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13H4Z" /><path d="M8 10h8m-8 4h5m-9 6h16" /></>,
    charts: <><path d="M4 20V13m5 7V9m5 11V5m5 15V11" /><path d="M3 20h18" /></>,
    collapse: <><path d="m13 6-6 6 6 6" /><path d="m19 6-6 6 6 6" /></>,
    expand: <><path d="m5 6 6 6-6 6" /><path d="m11 6 6 6-6 6" /></>,
    logout: <><path d="M10 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4" /><path d="m15 8 4 4-4 4M19 12H9" /></>,
    'chevron-left': <path d="m15 18-6-6 6-6" />,
    'chevron-right': <path d="m9 18 6-6-6-6" />,
  }

  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>
}
