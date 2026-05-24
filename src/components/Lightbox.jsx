import { useEffect } from 'react'
import './Lightbox.css'

export default function Lightbox({ photo, onClose, onPrev, onNext }) {
  useEffect(() => {
    function handleKey(event) {
      if (event.key === 'Escape') onClose()
      if (event.key === 'ArrowLeft') onPrev()
      if (event.key === 'ArrowRight') onNext()
    }
    window.addEventListener('keydown', handleKey)
    document.body.style.overflow = 'hidden'
    document.body.classList.add('has-lightbox')
    return () => {
      window.removeEventListener('keydown', handleKey)
      document.body.style.overflow = ''
      document.body.classList.remove('has-lightbox')
    }
  }, [onClose, onPrev, onNext])

  return (
    <div
      className="lightbox-backdrop"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-label={photo.caption}
    >
      <button
        type="button"
        className="lightbox-close"
        onClick={onClose}
        aria-label="Close photo"
      >
        ✕
      </button>

      <button
        type="button"
        className="lightbox-nav lightbox-prev"
        onClick={(e) => {
          e.stopPropagation()
          onPrev()
        }}
        aria-label="Previous photo"
      >
        ‹
      </button>

      <figure
        className="lightbox-figure"
        onClick={(e) => e.stopPropagation()}
      >
        {photo.src ? (
          <img className="lightbox-image" src={photo.src} alt={photo.caption} />
        ) : (
          <div
            className="lightbox-placeholder"
            style={{ background: photo.gradient }}
          >
            <span aria-hidden="true">{photo.emoji}</span>
          </div>
        )}
        <figcaption className="lightbox-caption">
          <span className="lightbox-text">{photo.caption}</span>
          <span className="lightbox-meta">
            {photo.category} · {photo.date}
          </span>
        </figcaption>
      </figure>

      <button
        type="button"
        className="lightbox-nav lightbox-next"
        onClick={(e) => {
          e.stopPropagation()
          onNext()
        }}
        aria-label="Next photo"
      >
        ›
      </button>
    </div>
  )
}
