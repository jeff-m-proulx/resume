import { useState, type ReactNode } from 'react'

interface ResumeSubsectionProps {
  title: string
  meta?: string
  initiallyExpanded?: boolean
  children: ReactNode
}

export function ResumeSubsection({
  title,
  meta,
  initiallyExpanded = false,
  children,
}: ResumeSubsectionProps) {
  const [expanded, setExpanded] = useState(initiallyExpanded)

  return (
    <div className="resume-subsection">
      <button
        className={`resume-subsection__header${expanded ? ' resume-subsection__header--expanded' : ''}`}
        onClick={() => setExpanded((prev) => !prev)}
        aria-expanded={expanded}
      >
        <span className="resume-subsection__label">
          <span className="resume-subsection__title">{title}</span>
          {meta && <span className="resume-subsection__meta">{meta}</span>}
        </span>
        <span className="resume-subsection__chevron">{expanded ? '▾' : '▸'}</span>
      </button>
      {expanded && <div className="resume-subsection__content">{children}</div>}
    </div>
  )
}
