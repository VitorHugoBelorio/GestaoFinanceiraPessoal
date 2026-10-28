import { Link } from 'react-router-dom'

function BrandContent() {
  return <><span className="brand-bars" aria-hidden="true"><i /><i /><i /></span><span>Contorno</span></>
}

export function Brand({ linked = true }: { linked?: boolean }) {
  if (!linked) return <div className="brand"><BrandContent /></div>
  return <Link className="brand" to="/"><BrandContent /></Link>
}
