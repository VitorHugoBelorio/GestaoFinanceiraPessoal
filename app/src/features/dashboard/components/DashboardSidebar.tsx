import type { KeyboardEvent } from 'react'
import { Brand } from '../../../components/ui/Brand'
import { ThemeToggle } from '../../../components/ui/ThemeToggle'
import type { DashboardTab } from '../dashboard.types'
import { DashboardIcon } from './DashboardIcon'

const tabs: ReadonlyArray<{ id: DashboardTab; label: string }> = [
  { id: 'overview', label: 'Visão geral' },
  { id: 'transactions', label: 'Lançamentos' },
  { id: 'charts', label: 'Gráficos' },
]

type DashboardSidebarProps = {
  activeTab: DashboardTab
  userName: string
  userEmail: string
  collapsed: boolean
  navigationOnly?: boolean
  onToggleCollapse(): void
  onLogout(): void
  onTabChange(tab: DashboardTab): void
}

export function DashboardSidebar({ activeTab, userName, userEmail, collapsed, navigationOnly = false, onToggleCollapse, onLogout, onTabChange }: DashboardSidebarProps) {
  const nameParts = userName.trim().split(/\s+/).filter(Boolean)
  const userInitials = `${nameParts[0]?.charAt(0) ?? 'U'}${nameParts.length > 1 ? nameParts[nameParts.length - 1].charAt(0) : ''}`.toUpperCase()

  function handleTabKeyDown(event: KeyboardEvent<HTMLButtonElement>, currentTab: DashboardTab) {
    const currentIndex = tabs.findIndex((tab) => tab.id === currentTab)
    let nextIndex: number | null = null

    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') nextIndex = (currentIndex + 1) % tabs.length
    if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') nextIndex = (currentIndex - 1 + tabs.length) % tabs.length
    if (event.key === 'Home') nextIndex = 0
    if (event.key === 'End') nextIndex = tabs.length - 1
    if (nextIndex === null) return

    event.preventDefault()
    const nextTab = tabs[nextIndex].id
    onTabChange(nextTab)
    document.getElementById(`dashboard-tab-${nextTab}`)?.focus()
  }

  return (
    <aside className={`sidebar${collapsed ? ' collapsed' : ''}`}>
      <div className="sidebar-top">
        <Brand linked={false} />
        <button className="sidebar-collapse" type="button" aria-controls="dashboard-navigation" aria-pressed={collapsed} aria-label={collapsed ? 'Expandir menu lateral' : 'Recolher menu lateral'} title={collapsed ? 'Expandir menu' : 'Recolher menu'} onClick={onToggleCollapse}><DashboardIcon name={collapsed ? 'expand' : 'collapse'} /></button>
      </div>
      <nav id="dashboard-navigation" className="app-nav" role={navigationOnly ? undefined : 'tablist'} aria-label="Áreas do dashboard">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            id={`dashboard-tab-${tab.id}`}
            role={navigationOnly ? undefined : 'tab'}
            tabIndex={navigationOnly ? 0 : activeTab === tab.id ? 0 : -1}
            aria-label={tab.label}
            title={collapsed ? tab.label : undefined}
            aria-controls={navigationOnly ? undefined : `dashboard-panel-${tab.id}`}
            aria-selected={navigationOnly ? undefined : activeTab === tab.id}
            aria-current={navigationOnly && tab.id === activeTab ? 'page' : undefined}
            className={activeTab === tab.id ? 'active' : ''}
            onKeyDown={navigationOnly ? undefined : (event) => handleTabKeyDown(event, tab.id)}
            onClick={() => onTabChange(tab.id)}
          >
            <span className="nav-icon"><DashboardIcon name={tab.id} /></span>
            <span className="nav-label">{tab.label}</span>
          </button>
        ))}
      </nav>
      <div className="sidebar-footer">
        <ThemeToggle compact={collapsed} />
        <div className="user-card" role="group" aria-label={collapsed ? `Conta de ${userName}, ${userEmail}` : undefined} title={`${userName} · ${userEmail}`}>
          <span className="user-avatar" aria-hidden="true">{userInitials}</span>
          <div className="user-details"><strong>{userName}</strong><small>{userEmail}</small></div>
        </div>
        <button className="button button-ghost sidebar-logout" type="button" aria-label="Sair da conta" title="Sair da conta" onClick={onLogout}><DashboardIcon name="logout" /><span>Sair da conta</span></button>
      </div>
    </aside>
  )
}
