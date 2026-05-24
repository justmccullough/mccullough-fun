import { useState } from 'react'
import { cows } from '../data/cows.js'
import PhotoCard from '../components/PhotoCard.jsx'
import Lightbox from '../components/Lightbox.jsx'
import './Cows.css'

export default function Cows() {
  const [lightboxIndex, setLightboxIndex] = useState(null)

  function showPrev() {
    setLightboxIndex((i) =>
      i === null ? i : (i - 1 + cows.length) % cows.length,
    )
  }

  function showNext() {
    setLightboxIndex((i) => (i === null ? i : (i + 1) % cows.length))
  }

  return (
    <div className="page cows">
      <header className="cows-head">
        <p className="cows-kicker">🐄 Moo-ving moments 🐄</p>
        <h1 className="cows-title">The Cow Corner</h1>
        <p className="cows-intro">
          The McCulloughs have never met a cow they didn’t like. Here’s our
          ever-growing herd of favorites — tap any one for a closer look.
        </p>
        <p className="cows-count">
          <strong>{cows.length}</strong> cows and counting 🐮
        </p>
      </header>

      <div className="photo-grid">
        {cows.map((cow, index) => (
          <PhotoCard
            key={cow.id}
            photo={cow}
            onOpen={() => setLightboxIndex(index)}
          />
        ))}
      </div>

      {lightboxIndex !== null && (
        <Lightbox
          photo={cows[lightboxIndex]}
          onClose={() => setLightboxIndex(null)}
          onPrev={showPrev}
          onNext={showNext}
        />
      )}
    </div>
  )
}
