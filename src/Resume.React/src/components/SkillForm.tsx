import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Skill } from '../types'

interface Props {
  editingSkill: Skill | null
  onSaved: () => void
}

export function SkillForm({ editingSkill, onSaved }: Props) {
  const [category, setCategory] = useState(editingSkill?.category ?? '')
  const [name, setName] = useState(editingSkill?.name ?? '')
  const [sortOrder, setSortOrder] = useState(editingSkill?.sortOrder ?? 0)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      if (editingSkill) {
        await api.updateSkill({ id: editingSkill.id, category, name, sortOrder })
      } else {
        await api.createSkill({ category, name, sortOrder })
      }
      setError(null)
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Category
          <input value={category} onChange={(e) => setCategory(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Sort order
          <input type="number" value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value))} />
        </label>
      </div>
      <button type="submit">{editingSkill ? 'Save' : 'Add'}</button>
      {error && <p className="form-error">{error}</p>}
    </form>
  )
}
