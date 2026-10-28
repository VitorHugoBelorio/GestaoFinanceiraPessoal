import { useEffect, useState } from 'react'

export function useSidebarCollapsed() {
  const [collapsed, setCollapsed] = useState(() => {
    try { return window.localStorage.getItem('contorno.sidebar.collapsed') === 'true' }
    catch { return false }
  })

  useEffect(() => {
    try { window.localStorage.setItem('contorno.sidebar.collapsed', String(collapsed)) }
    catch { /* O menu continua funcional sem armazenamento local. */ }
  }, [collapsed])

  return [collapsed, setCollapsed] as const
}
