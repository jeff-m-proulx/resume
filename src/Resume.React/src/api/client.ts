import type { PersonalInfo, Skill, Experience, Education } from '../types'

// Dev sets VITE_API_URL to the API's endpoint and calls it cross-origin. In
// the deployed container it is unset, so requests go same-origin to /api/...
// and nginx proxies them — no API host is baked into the bundle at build time.
const API_BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? ''

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

export const api = {
  getPersonalInfo: () => request<PersonalInfo>('/api/personal-info'),
  updatePersonalInfo: (body: Omit<PersonalInfo, 'id'>) =>
    request<PersonalInfo>('/api/personal-info', { method: 'PUT', body: JSON.stringify(body) }),

  getSkills: () => request<Skill[]>('/api/skills'),
  createSkill: (body: Omit<Skill, 'id'>) =>
    request<Skill>('/api/skills', { method: 'POST', body: JSON.stringify(body) }),
  updateSkill: (skill: Skill) =>
    request<Skill>(`/api/skills/${skill.id}`, { method: 'PUT', body: JSON.stringify(skill) }),

  getExperience: () => request<Experience[]>('/api/experience'),
  createExperience: (body: Omit<Experience, 'id'>) =>
    request<Experience>('/api/experience', { method: 'POST', body: JSON.stringify(body) }),
  updateExperience: (entry: Experience) =>
    request<Experience>(`/api/experience/${entry.id}`, { method: 'PUT', body: JSON.stringify(entry) }),

  getEducation: () => request<Education[]>('/api/education'),
  createEducation: (body: Omit<Education, 'id'>) =>
    request<Education>('/api/education', { method: 'POST', body: JSON.stringify(body) }),
  updateEducation: (entry: Education) =>
    request<Education>(`/api/education/${entry.id}`, { method: 'PUT', body: JSON.stringify(entry) }),
}
