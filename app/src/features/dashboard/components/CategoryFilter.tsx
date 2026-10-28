import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import type { Category } from '../../../types/api'

type CategoryFilterProps = {
  categories: Category[]
  value: string
  onChange(value: string): void
}

export function CategoryFilter({ categories, value, onChange }: CategoryFilterProps) {
  const [isOpen, setIsOpen] = useState(false)
  const fieldId = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const optionRefs = useRef<Array<HTMLButtonElement | null>>([])
  const options = [
    { value: 'all', label: 'Todos os lançamentos' },
    { value: 'none', label: 'Sem etiqueta' },
    ...categories.map((category) => ({
      value: String(category.id),
      label: `${category.type === 'Income' ? 'Receita' : 'Despesa'} · ${category.name}`,
    })),
  ]
  const selectedName = options.find((option) => option.value === value)?.label ?? options[0].label

  useEffect(() => {
    if (!isOpen) return
    const selectedIndex = options.findIndex((option) => option.value === value)
    optionRefs.current[selectedIndex >= 0 ? selectedIndex : 0]?.focus()

    function handleOutsideClick(event: PointerEvent) {
      if (event.target instanceof Node && !rootRef.current?.contains(event.target)) setIsOpen(false)
    }

    document.addEventListener('pointerdown', handleOutsideClick)
    return () => document.removeEventListener('pointerdown', handleOutsideClick)
  }, [isOpen, value, categories])

  function handleOptionsKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const currentIndex = optionRefs.current.findIndex((option) => option === document.activeElement)
    let nextIndex: number | null = null
    if (event.key === 'ArrowDown') nextIndex = (currentIndex + 1) % options.length
    if (event.key === 'ArrowUp') nextIndex = (currentIndex - 1 + options.length) % options.length
    if (event.key === 'Home') nextIndex = 0
    if (event.key === 'End') nextIndex = options.length - 1
    if (nextIndex !== null) {
      event.preventDefault()
      optionRefs.current[nextIndex]?.focus()
    }
    if (event.key === 'Escape') {
      event.preventDefault()
      setIsOpen(false)
      triggerRef.current?.focus()
    }
    if (event.key === 'Tab') setIsOpen(false)
  }

  return (
    <div className="category-select category-filter-select" ref={rootRef}>
      <span className="category-select-label" id={`${fieldId}-label`}>Filtrar por etiqueta</span>
      <button
        ref={triggerRef}
        className="category-select-trigger"
        type="button"
        aria-labelledby={`${fieldId}-label ${fieldId}-value`}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={isOpen ? `${fieldId}-options` : undefined}
        onClick={() => setIsOpen((current) => !current)}
        onKeyDown={(event) => {
          if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault()
            setIsOpen(true)
          }
        }}
      >
        <span id={`${fieldId}-value`}>{selectedName}</span>
        <svg aria-hidden="true" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><path d="m4 6 4 4 4-4" /></svg>
      </button>
      {isOpen && (
        <div className="category-options" id={`${fieldId}-options`} role="listbox" aria-labelledby={`${fieldId}-label`} onKeyDown={handleOptionsKeyDown}>
          {options.map((option, index) => (
            <button
              key={option.value}
              ref={(element) => { optionRefs.current[index] = element }}
              className="category-option"
              type="button"
              role="option"
              aria-selected={option.value === value}
              onClick={() => {
                onChange(option.value)
                setIsOpen(false)
                triggerRef.current?.focus()
              }}
            >{option.label}</button>
          ))}
        </div>
      )}
    </div>
  )
}
