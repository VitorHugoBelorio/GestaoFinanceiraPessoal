import { useLayoutEffect, useRef } from 'react'
import { formatMoneyInput } from '../../utils/money'

type CurrencyInputProps = {
  value: string
  onValueChange(value: string): void
}

export function CurrencyInput({ value, onValueChange }: CurrencyInputProps) {
  const inputRef = useRef<HTMLInputElement>(null)

  function moveCaretToEnd() {
    const input = inputRef.current
    if (input) input.setSelectionRange(input.value.length, input.value.length)
  }

  useLayoutEffect(() => {
    if (document.activeElement === inputRef.current) moveCaretToEnd()
  }, [value])

  return (
    <span className="money-input">
      <span aria-hidden="true">R$</span>
      <input
        ref={inputRef}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        value={value || '0,00'}
        onChange={(event) => onValueChange(formatMoneyInput(event.currentTarget.value))}
        onFocus={moveCaretToEnd}
        onClick={moveCaretToEnd}
      />
    </span>
  )
}
