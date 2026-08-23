export interface PersonalInfo {
  id: string;
  fullName: string;
  headline: string;
  email: string;
  phone: string | null;
  summary: string;
  linkedInUrl: string | null;
  gitHubUrl: string | null;
  websiteUrl: string | null;
}

export interface Skill {
  id: string;
  category: string;
  name: string;
  sortOrder: number;
}

export interface Experience {
  id: string;
  company: string;
  jobTitle: string;
  location: string | null;
  startDate: string;
  endDate: string | null;
  highlights: string[];
}

export interface Education {
  id: string;
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  startDate: string;
  endDate: string | null;
  details: string[];
}
