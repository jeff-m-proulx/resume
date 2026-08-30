import { useEffect, useRef, useState } from 'react'
import { api } from './api/client'
import type { PersonalInfo, Skill, Experience, Education } from './types'
import { ResumeSection } from './components/ResumeSection'
import { PersonalInfoForm } from './components/PersonalInfoForm'
import { SkillForm } from './components/SkillForm'
import { ExperienceForm } from './components/ExperienceForm'
import { EducationForm } from './components/EducationForm'
import './App.css'

const SAVED_BANNER = 'Saved — but this is a demo; changes are not actually persisted.'

function App() {
  const [personalInfo, setPersonalInfo] = useState<PersonalInfo | null>(null)
  const [skills, setSkills] = useState<Skill[]>([])
  const [experience, setExperience] = useState<Experience[]>([])
  const [education, setEducation] = useState<Education[]>([])
  const [adminMode, setAdminMode] = useState(false)
  const [banner, setBanner] = useState<string | null>(null)
  const [editingSkill, setEditingSkill] = useState<Skill | null>(null)
  const [editingExperience, setEditingExperience] = useState<Experience | null>(null)
  const [editingEducation, setEditingEducation] = useState<Education | null>(null)
  const headerRef = useRef<HTMLElement>(null)

  const loadAll = async () => {
    setPersonalInfo(await api.getPersonalInfo())
    setSkills(await api.getSkills())
    setExperience(await api.getExperience())
    setEducation(await api.getEducation())
  }

  useEffect(() => {
    loadAll()
  }, [])

  // Section headers dock beneath the sticky page header, so they need its
  // height as a CSS variable. It changes with viewport width as the contact
  // line wraps, hence the observer rather than a one-time measurement.
  useEffect(() => {
    const header = headerRef.current
    if (!header) {
      return
    }

    const publishHeight = () => {
      document.documentElement.style.setProperty(
        '--resume-header-height',
        `${header.getBoundingClientRect().height}px`,
      )
    }

    publishHeight()

    const observer = new ResizeObserver(publishHeight)
    observer.observe(header)
    return () => observer.disconnect()
  }, [personalInfo])

  const handleSaved = async () => {
    setEditingSkill(null)
    setEditingExperience(null)
    setEditingEducation(null)
    setBanner(SAVED_BANNER)
    await loadAll()
  }

  if (!personalInfo) {
    return <p>Loading...</p>
  }

  const skillsByCategory = skills.reduce<Record<string, Skill[]>>((groups, skill) => {
    ;(groups[skill.category] ??= []).push(skill)
    return groups
  }, {})

  return (
    <div className="resume-page">
      <header className="resume-header" ref={headerRef}>
        <h1>{personalInfo.fullName}</h1>
        <p className="resume-header__headline">{personalInfo.headline}</p>
        <p className="resume-header__contact">
          {[personalInfo.location, personalInfo.phone, personalInfo.email]
            .filter(Boolean)
            .map((value, index) => (
              <span key={value}>
                {index > 0 && <span className="resume-header__separator">   |   </span>}
                {value}
              </span>
            ))}
        </p>
      </header>

      <div className="resume-intro">
        <p>{personalInfo.summary}</p>
        {adminMode && <PersonalInfoForm personalInfo={personalInfo} onSaved={handleSaved} />}
      </div>

      <div className="resume-page__toolbar">
        <button onClick={() => setAdminMode((prev) => !prev)}>
          {adminMode ? 'Exit Admin Mode' : 'Admin Mode'}
        </button>
      </div>

      <ResumeSection title="Skills">
        {Object.entries(skillsByCategory).map(([category, items]) => (
          <div key={category}>
            <h4>{category}</h4>
            <ul>
              {items
                .slice()
                .sort((a, b) => a.sortOrder - b.sortOrder)
                .map((skill) => (
                  <li key={skill.id}>
                    {skill.name}
                    {adminMode && <button onClick={() => setEditingSkill(skill)}>Edit</button>}
                  </li>
                ))}
            </ul>
          </div>
        ))}
        {adminMode && (
          <SkillForm key={editingSkill?.id ?? 'new'} editingSkill={editingSkill} onSaved={handleSaved} />
        )}
      </ResumeSection>

      <ResumeSection title="Experience">
        {experience.map((job) => (
          <div className="resume-entry" key={job.id}>
            <h4>
              {job.jobTitle}, {job.company}
              {adminMode && <button onClick={() => setEditingExperience(job)}>Edit</button>}
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
        {adminMode && (
          <ExperienceForm
            key={editingExperience?.id ?? 'new'}
            editingEntry={editingExperience}
            onSaved={handleSaved}
          />
        )}
      </ResumeSection>

      <ResumeSection title="Education">
        {education.map((edu) => (
          <div className="resume-entry" key={edu.id}>
            <h4>
              {edu.degree}, {edu.institution}
              {adminMode && <button onClick={() => setEditingEducation(edu)}>Edit</button>}
            </h4>
            <p>
              {edu.startDate} - {edu.endDate ?? 'Present'}
            </p>
          </div>
        ))}
        {adminMode && (
          <EducationForm
            key={editingEducation?.id ?? 'new'}
            editingEntry={editingEducation}
            onSaved={handleSaved}
          />
        )}
      </ResumeSection>

      {banner && <div className="resume-banner">{banner}</div>}
    </div>
  )
}

export default App
