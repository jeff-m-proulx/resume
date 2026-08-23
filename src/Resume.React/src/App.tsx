import { useEffect, useState } from 'react'
import { api } from './api/client'
import type { PersonalInfo } from './types'
import './App.css'

function App() {
  const [personalInfo, setPersonalInfo] = useState<PersonalInfo | null>(null)

  useEffect(() => {
    api.getPersonalInfo().then(setPersonalInfo)
  }, [])

  if (!personalInfo) {
    return <p>Loading...</p>
  }

  return (
    <div>
      <h1>{personalInfo.fullName}</h1>
      <p>{personalInfo.headline}</p>
    </div>
  )
}

export default App
