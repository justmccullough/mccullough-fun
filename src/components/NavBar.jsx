import { NavLink } from 'react-router-dom'

export default function NavBar() {
  return (
    <header className="navbar">
      <NavLink to="/" className="navbar-brand">
        <span className="navbar-logo" aria-hidden="true">🏡</span>
        <span className="navbar-name">
          mccullough<span className="navbar-dot">.fun</span>
        </span>
      </NavLink>
      <nav className="navbar-links" aria-label="Main navigation">
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
