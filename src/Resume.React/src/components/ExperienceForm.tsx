import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Experience } from '../types'

interface Props {
  editingEntry: Experience | null
  onSaved: () => void
}

export function ExperienceForm({ editingEntry, onSaved }: Props) {
  const [company, setCompany] = useState(editingEntry?.company ?? '')
  const [jobTitle, setJobTitle] = useState(editingEntry?.jobTitle ?? '')
  const [startDate, setStartDate] = useState(editingEntry?.startDate ?? new Date().toISOString().slice(0, 10))
  const [highlightsText, setHighlightsText] = useState(editingEntry?.highlights.join('\n') ?? '')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const highlights = highlightsText.split('\n').map((h) => h.trim()).filter(Boolean)

    if (editingEntry) {
      await api.updateExperience({ ...editingEntry, company, jobTitle, startDate, highlights })
    } else {
      await api.createExperience({ company, jobTitle, location: null, startDate, endDate: null, highlights })
    }
    onSaved()
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Company
          <input value={company} onChange={(e) => setCompany(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Job title
          <input value={jobTitle} onChange={(e) => setJobTitle(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Start date
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Highlights (one per line)
          <textarea value={highlightsText} onChange={(e) => setHighlightsText(e.target.value)} />
        </label>
      </div>
      <button type="submit">{editingEntry ? 'Save' : 'Add'}</button>
    </form>
  )
}
