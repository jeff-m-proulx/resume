import { useEffect, useState } from 'react'
import { api } from './api/client'
import type { PersonalInfo, Skill, Experience, Education } from './types'
import { ResumeSection } from './components/ResumeSection'
import './App.css'

function App() {
  const [personalInfo, setPersonalInfo] = useState<PersonalInfo | null>(null)
  const [skills, setSkills] = useState<Skill[]>([])
  const [experience, setExperience] = useState<Experience[]>([])
  const [education, setEducation] = useState<Education[]>([])

  const loadAll = async () => {
    setPersonalInfo(await api.getPersonalInfo())
    setSkills(await api.getSkills())
    setExperience(await api.getExperience())
    setEducation(await api.getEducation())
  }

  useEffect(() => {
    loadAll()
  }, [])

  if (!personalInfo) {
    return <p>Loading...</p>
  }

  const skillsByCategory = skills.reduce<Record<string, Skill[]>>((groups, skill) => {
    ;(groups[skill.category] ??= []).push(skill)
    return groups
  }, {})

  return (
    <div className="resume-page">
      <ResumeSection title="Personal Info" initiallyExpanded>
        <p>
          <strong>{personalInfo.fullName}</strong> — {personalInfo.headline}
        </p>
        <p>{personalInfo.summary}</p>
        <p>
          {personalInfo.email}
          {personalInfo.phone && ` • ${personalInfo.phone}`}
        </p>
      </ResumeSection>

      <ResumeSection title="Skills">
        {Object.entries(skillsByCategory).map(([category, items]) => (
          <div key={category}>
            <h4>{category}</h4>
            <ul>
              {items
                .slice()
                .sort((a, b) => a.sortOrder - b.sortOrder)
                .map((skill) => (
                  <li key={skill.id}>{skill.name}</li>
                ))}
            </ul>
          </div>
        ))}
      </ResumeSection>

      <ResumeSection title="Experience">
        {experience.map((job) => (
          <div className="resume-entry" key={job.id}>
            <h4>
              {job.jobTitle}, {job.company}
            </h4>
            <p>
              {job.startDate} - {job.endDate ?? 'Present'}
            </p>
            <ul>
              {job.highlights.map((highlight, index) => (
                <li key={index}>{highlight}</li>
              ))}
            </ul>
          </div>
        ))}
      </ResumeSection>

      <ResumeSection title="Education">
        {education.map((edu) => (
          <div className="resume-entry" key={edu.id}>
            <h4>
              {edu.degree}, {edu.institution}
            </h4>
            <p>
              {edu.startDate} - {edu.endDate ?? 'Present'}
            </p>
          </div>
        ))}
      </ResumeSection>
    </div>
  )
}

export default App
