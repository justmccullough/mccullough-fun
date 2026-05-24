import { Link } from 'react-router-dom'

export default function NotFound() {
  return (
    <div className="page" style={{ textAlign: 'center' }}>
      <p style={{ fontSize: '3.5rem' }} aria-hidden="true">🧭🏡</p>
      <h1 style={{ fontSize: '2.4rem' }}>This room isn't in the house</h1>
      <p style={{ color: 'var(--ink-soft)', margin: '0.6rem 0 1.6rem' }}>
        Looks like you wandered down a hallway that doesn't exist yet.
      </p>
      <Link to="/" className="btn">
        Take me home
      </Link>
    </div>
  )
}
