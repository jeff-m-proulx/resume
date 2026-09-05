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

  // The Blazor app gets this from <FocusOnNavigate Selector="h1" />. Without it,
  // clicking the footer link at the bottom of a long resume leaves the visitor
  // scrolled to the bottom of the shorter About page, and a screen reader never
  // learns the view changed.
  useEffect(() => {
    window.scrollTo(0, 0)

    const heading = document.querySelector('h1')
    if (heading) {
      heading.tabIndex = -1
      heading.focus()
    }
  }, [route])

  return (
    <>
      {route === ABOUT_ROUTE ? <About /> : <ResumePage />}
      <ResumeFooter />
    </>
  )
}

export default App
