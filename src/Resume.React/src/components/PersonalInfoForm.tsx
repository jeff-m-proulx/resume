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
  const [email, setEmail] = useState(personalInfo.email)
  const [summary, setSummary] = useState(personalInfo.summary)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await api.updatePersonalInfo({ ...personalInfo, fullName, headline, email, summary })
    onSaved()
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
    </form>
  )
}
