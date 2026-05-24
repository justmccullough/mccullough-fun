import { useState } from 'react'
import { Link } from 'react-router-dom'
import { familyFacts } from '../data/familyFacts.js'
import './Home.css'

const traditions = [
  {
    emoji: '🥞',
    title: 'Sunday Pancakes',
    note: 'Flipped with great confidence and varying success.',
  },
  {
    emoji: '🎲',
    title: 'Game Nights',
    note: 'Fiercely competitive, lovingly chaotic.',
  },
  {
    emoji: '🚗',
    title: 'Summer Road Trips',
    note: 'Snacks packed, playlist ready, map optional.',
  },
  {
    emoji: '🎄',
    title: 'Holiday Gatherings',
    note: 'The whole crew, one very full kitchen.',
  },
]

export default function Home() {
  const [factIndex, setFactIndex] = useState(
    () => Math.floor(Math.random() * familyFacts.length),
  )

  function shuffleFact() {
    setFactIndex((current) => {
      if (familyFacts.length < 2) return current
      let next = current
      while (next === current) {
        next = Math.floor(Math.random() * familyFacts.length)
      }
      return next
    })
  }

  return (
    <div className="page home">
      <section className="hero">
        <p className="hero-kicker">Welcome to</p>
        <h1 className="hero-title">The McCullough Family</h1>
        <p className="hero-tagline">
          A cozy little corner of the internet — full of photos, memories, and
          the people we love most.
        </p>
        <div className="hero-actions">
          <Link to="/gallery" className="btn">
            Browse our photos
          </Link>
          <a href="#traditions" className="btn btn-ghost">
            Our little traditions
          </a>
        </div>
      </section>

      <section className="welcome-note" aria-label="A note from the family">
        <h2 className="welcome-heading">Pull up a chair</h2>
        <p className="welcome-text">
          We built this little site to keep our favorite moments in one warm
          place. Whether you’re family, an old friend, or someone who just
          wandered in — you’re always welcome here. Kick off your shoes, stay a
          while, and have a look around.
        </p>
        <p className="welcome-sign">— with love, the McCulloughs</p>
      </section>

      <section className="fact-card" aria-live="polite">
        <span className="fact-label">Family Fun Fact</span>
        <p className="fact-text">{familyFacts[factIndex]}</p>
        <button type="button" className="btn btn-ghost" onClick={shuffleFact}>
          🎲 Tell me another
        </button>
      </section>

      <section id="traditions" className="traditions">
        <h2 className="section-heading">Our Little Traditions</h2>
        <div className="tradition-grid">
          {traditions.map((item) => (
            <article key={item.title} className="tradition-card">
              <span className="tradition-emoji" aria-hidden="true">
                {item.emoji}
              </span>
              <h3 className="tradition-title">{item.title}</h3>
              <p className="tradition-note">{item.note}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="cta-strip">
        <h2 className="cta-heading">Come see the good stuff</h2>
        <p className="cta-text">
          Years of birthdays, holidays, and ordinary perfect days — all in one
          album.
        </p>
        <Link to="/gallery" className="btn">
          Open the photo album
        </Link>
      </section>
    </div>
  )
}
