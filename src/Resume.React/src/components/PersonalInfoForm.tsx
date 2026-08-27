import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { PersonalInfo } from '../types'

interface Props {
  personalInfo: PersonalInfo
  onSaved: () => void
}

export function PersonalInfoForm({ personalInfo, onSaved }: Props) {
  const [fullName, setFullName] = useState(personalInfo.fullName)
  const [headline, setHeadline] = useState(personalInfo.headline)
  const [location, setLocation] = useState(personalInfo.location ?? '')
  const [email, setEmail] = useState(personalInfo.email)
  const [summary, setSummary] = useState(personalInfo.summary)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      await api.updatePersonalInfo({ ...personalInfo, fullName, headline, location: location || null, email, summary })
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
          Full name
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Headline
          <input value={headline} onChange={(e) => setHeadline(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Location
          <input value={location} onChange={(e) => setLocation(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Email
          <input value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Summary
          <textarea value={summary} onChange={(e) => setSummary(e.target.value)} />
        </label>
      </div>
      <button type="submit">Save</button>
      {error && <p className="form-error">{error}</p>}
    </form>
  )
}
