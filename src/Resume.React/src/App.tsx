import { useEffect, useState } from 'react'
import { ResumePage } from './pages/ResumePage'
import { About } from './pages/About'
import { ResumeFooter } from './components/ResumeFooter'
import './App.css'

const ABOUT_ROUTE = '#/about'

// A hash rather than a path: nginx serves this bundle in production, and a hash
// needs no server involvement at all -- no rewrite rules, and no chance of
// colliding with the /api/ or /switch-ui locations.
function App() {
  const [route, setRoute] = useState(() => window.location.hash)

  useEffect(() => {
    const handleHashChange = () => setRoute(window.location.hash)
    window.addEventListener('hashchange', handleHashChange)
    return () => window.removeEventListener('hashchange', handleHashChange)
  }, [])

  return (
    <>
      {route === ABOUT_ROUTE ? <About /> : <ResumePage />}
      <ResumeFooter />
    </>
  )
}

export default App
