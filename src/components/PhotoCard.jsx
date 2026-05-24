import './PhotoCard.css'

// Small deterministic tilt so each polaroid leans a little differently.
function tiltFor(id) {
  const tilts = [-3, 2, -1.5, 3, -2.5, 1.5]
  return tilts[id % tilts.length]
}

export default function PhotoCard({ photo, onOpen }) {
  return (
    <button
      type="button"
      className="photo-card"
      style={{ '--tilt': `${tiltFor(photo.id)}deg` }}
      onClick={onOpen}
      aria-label={`Open photo: ${photo.caption}`}
    >
      <div className="photo-frame">
        {photo.src ? (
          <img className="photo-image" src={photo.src} alt={photo.caption} />
        ) : (
          <div
            className="photo-placeholder"
            style={{ background: photo.gradient }}
          >
            <span className="photo-emoji" aria-hidden="true">
              {photo.emoji}
            </span>
          </div>
        )}
      </div>
      <p className="photo-caption">{photo.caption}</p>
      <p className="photo-date">{photo.date}</p>
    </button>
  )
}
