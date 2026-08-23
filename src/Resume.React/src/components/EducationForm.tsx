import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Education } from '../types'

interface Props {
  editingEntry: Education | null
  onSaved: () => void
}

export function EducationForm({ editingEntry, onSaved }: Props) {
  const [institution, setInstitution] = useState(editingEntry?.institution ?? '')
  const [degree, setDegree] = useState(editingEntry?.degree ?? '')
  const [startDate, setStartDate] = useState(editingEntry?.startDate ?? new Date().toISOString().slice(0, 10))

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (editingEntry) {
      await api.updateEducation({ ...editingEntry, institution, degree, startDate })
    } else {
      await api.createEducation({ institution, degree, fieldOfStudy: null, startDate, endDate: null, details: [] })
    }
    onSaved()
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Institution
          <input value={institution} onChange={(e) => setInstitution(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Degree
          <input value={degree} onChange={(e) => setDegree(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Start date
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        </label>
      </div>
      <button type="submit">{editingEntry ? 'Save' : 'Add'}</button>
    </form>
  )
}
