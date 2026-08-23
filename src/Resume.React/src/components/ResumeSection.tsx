import { useState, type ReactNode } from 'react'

interface ResumeSectionProps {
  title: string
  initiallyExpanded?: boolean
  children: ReactNode
}

export function ResumeSection({ title, initiallyExpanded = false, children }: ResumeSectionProps) {
  const [expanded, setExpanded] = useState(initiallyExpanded)

  return (
    <div className="resume-section">
      <button
        className="resume-section__header"
        onClick={() => setExpanded((prev) => !prev)}
        aria-expanded={expanded}
      >
        <span>{title}</span>
        <span className="resume-section__chevron">{expanded ? '▾' : '▸'}</span>
      </button>
      {expanded && <div className="resume-section__content">{children}</div>}
    </div>
  )
}
