export function parseMoney(value: string): number {
  const normalized = value.trim().replace(/\s/g, '')

  if (normalized.includes(',')) {
    return Number(normalized.replace(/\./g, '').replace(',', '.'))
  }

  return Number(normalized)
}

// Digitação monetária: 1 → 0,01; 12 → 0,12; 123 → 1,23.
export function formatMoneyInput(value: string): string {
  const digits = value.replace(/\D/g, '').replace(/^0+(?=\d)/, '').slice(0, 15)
  if (!digits) return ''

  const padded = digits.padStart(3, '0')
  const integer = padded.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
  return `${integer},${padded.slice(-2)}`
}
