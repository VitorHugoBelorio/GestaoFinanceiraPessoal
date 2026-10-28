import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { ApiError, apiRequest } from '../../../api/client'
import type { Category, TransactionType } from '../../../types/api'

type CategoryPickerProps = {
  type: TransactionType
  categories: Category[]
  value: number | null
  onChange(categoryId: number | null): void
  onCreated(category: Category): void
}

export function CategoryPicker({ type, categories, value, onChange, onCreated }: CategoryPickerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [name, setName] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const fieldId = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const optionRefs = useRef<Array<HTMLButtonElement | null>>([])
  const options = [{ id: null as number | null, name: 'Sem etiqueta' }, ...categories.filter((category) => category.type === type)]
  const selectedName = options.find((option) => option.id === value)?.name ?? 'Sem etiqueta'

  useEffect(() => {
    if (!isOpen) return
    optionRefs.current[options.findIndex((option) => option.id === value)]?.focus()
    function handleOutsideClick(event: PointerEvent) {
      if (event.target instanceof Node && !rootRef.current?.contains(event.target)) setIsOpen(false)
    }
    document.addEventListener('pointerdown', handleOutsideClick)
    return () => document.removeEventListener('pointerdown', handleOutsideClick)
  }, [isOpen, value, categories, type])

  function handleOptionsKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const currentIndex = optionRefs.current.findIndex((option) => option === document.activeElement)
    let nextIndex: number | null = null
    if (event.key === 'ArrowDown') nextIndex = (currentIndex + 1) % options.length
    if (event.key === 'ArrowUp') nextIndex = (currentIndex - 1 + options.length) % options.length
    if (event.key === 'Home') nextIndex = 0
    if (event.key === 'End') nextIndex = options.length - 1
    if (nextIndex !== null) { event.preventDefault(); optionRefs.current[nextIndex]?.focus() }
    if (event.key === 'Escape') { event.preventDefault(); setIsOpen(false); triggerRef.current?.focus() }
    if (event.key === 'Tab') setIsOpen(false)
  }

  async function createCategory() {
    const normalizedName = name.trim()
    if (normalizedName.length < 2) {
      setError('A etiqueta precisa ter pelo menos 2 caracteres.')
      return
    }

    setSaving(true)
    setError('')
    try {
      const category = await apiRequest<Category>('/categories', {
        method: 'POST',
        body: JSON.stringify({ name: normalizedName, type }),
      })
      onCreated(category)
      onChange(category.id)
      setName('')
      setIsOpen(false)
      setIsCreating(false)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível criar a etiqueta.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="category-picker">
      <div className="category-field">
        <div className="category-select" ref={rootRef}>
          <span className="category-select-label" id={`${fieldId}-label`}>Etiqueta</span>
          <button ref={triggerRef} className="category-select-trigger" type="button" aria-labelledby={`${fieldId}-label ${fieldId}-value`} aria-haspopup="listbox" aria-expanded={isOpen} aria-controls={isOpen ? `${fieldId}-options` : undefined} onClick={() => setIsOpen((current) => !current)} onKeyDown={(event) => { if (event.key === 'ArrowDown' || event.key === 'ArrowUp') { event.preventDefault(); setIsOpen(true) } }}><span id={`${fieldId}-value`}>{selectedName}</span><svg aria-hidden="true" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><path d="m4 6 4 4 4-4" /></svg></button>
          {isOpen && <div className="category-options" id={`${fieldId}-options`} role="listbox" aria-labelledby={`${fieldId}-label`} onKeyDown={handleOptionsKeyDown}>{options.map((option, index) => <button key={option.id ?? 'none'} ref={(element) => { optionRefs.current[index] = element }} className="category-option" type="button" role="option" aria-selected={option.id === value} onClick={() => { onChange(option.id); setIsOpen(false); triggerRef.current?.focus() }}>{option.name}</button>)}</div>}
        </div>
        <button className="category-create-toggle" type="button" aria-expanded={isCreating} onClick={() => { setError(''); setIsOpen(false); setIsCreating((current) => !current) }}><span aria-hidden="true">{isCreating ? '×' : '+'}</span>{isCreating ? 'Fechar criação' : 'Criar etiqueta'}</button>
      </div>
      {isCreating && <div className="category-create"><label>Nome da nova etiqueta<input maxLength={80} value={name} onChange={(event) => setName(event.target.value)} placeholder={type === 'Income' ? 'Ex.: Salário' : 'Ex.: Mercado'} onKeyDown={(event) => { if (event.key === 'Enter') { event.preventDefault(); void createCategory() } }} /></label><button className="category-create-save" type="button" disabled={saving} onClick={() => { void createCategory() }}>{saving ? 'Criando...' : 'Salvar etiqueta'}</button></div>}
      {error && <div className="form-error" role="alert">{error}</div>}
    </div>
  )
}
