export function LoadingState({ label = 'Carregando...' }: { label?: string }) {
  return (
    <div className="page-status" role="status" aria-live="polite">
      <span className="loading-mark" aria-hidden="true"><i /><i /><i /></span>
      <span>{label}</span>
    </div>
  )
}
