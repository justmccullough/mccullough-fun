import { useEffect, useRef } from 'react'
import { NavLink, useLocation } from 'react-router-dom'

export default function NavBar() {
  const linksRef = useRef(null)
  const { pathname } = useLocation()

  // On phones the links are one swipeable row; keep the current page's link in view.
  useEffect(() => {
    const links = linksRef.current
    const active = links?.querySelector('.is-active')
    if (!active || links.scrollWidth <= links.clientWidth) return
    const left = active.getBoundingClientRect().left - links.getBoundingClientRect().left + links.scrollLeft
    links.scrollLeft = left - (links.clientWidth - active.offsetWidth) / 2
  }, [pathname])

  return (
    <header className="navbar">
      <NavLink to="/" className="navbar-brand">
        <span className="navbar-logo" aria-hidden="true">🏡</span>
        <span className="navbar-name">
          mccullough<span className="navbar-dot">.fun</span>
        </span>
      </NavLink>
      <nav ref={linksRef} className="navbar-links" aria-label="Main navigation">
        <NavLink
          to="/"
          end
          className={({ isActive }) =>
            isActive ? 'navbar-link is-active' : 'navbar-link'
          }
        >
          Home
        </NavLink>
        <NavLink
          to="/gallery"
          className={({ isActive }) =>
            isActive ? 'navbar-link is-active' : 'navbar-link'
          }
        >
          Photos
        </NavLink>
        <NavLink
          to="/cows"
          className={({ isActive }) =>
            isActive ? 'navbar-link is-active' : 'navbar-link'
          }
        >
          Cows
        </NavLink>
        <NavLink
          to="/pong"
          className={({ isActive }) =>
            isActive ? 'navbar-link is-active' : 'navbar-link'
          }
        >
          Pong
        </NavLink>
        <NavLink
          to="/mooquest"
          className={({ isActive }) =>
            isActive ? 'navbar-link is-active' : 'navbar-link'
          }
        >
          Moo Quest
        </NavLink>
        <NavLink
          to="/christmas"
          className={({ isActive }) =>
            isActive ? 'navbar-link is-active' : 'navbar-link'
          }
        >
          Christmas
        </NavLink>
      </nav>
    </header>
  )
}
