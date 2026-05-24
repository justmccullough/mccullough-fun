import { useMemo, useState } from 'react'
import { photos, categories } from '../data/photos.js'
import PhotoCard from '../components/PhotoCard.jsx'
import Lightbox from '../components/Lightbox.jsx'
import './Gallery.css'

export default function Gallery() {
  const [activeCategory, setActiveCategory] = useState('All')
  const [lightboxIndex, setLightboxIndex] = useState(null)

  const visiblePhotos = useMemo(() => {
    if (activeCategory === 'All') return photos
    return photos.filter((p) => p.category === activeCategory)
  }, [activeCategory])

  function selectCategory(category) {
    setActiveCategory(category)
    setLightboxIndex(null)
  }

  function showPrev() {
    setLightboxIndex((i) =>
      i === null ? i : (i - 1 + visiblePhotos.length) % visiblePhotos.length,
    )
  }

  function showNext() {
    setLightboxIndex((i) =>
      i === null ? i : (i + 1) % visiblePhotos.length,
    )
  }

  return (
    <div className="page gallery">
      <header className="gallery-head">
        <p className="gallery-kicker">The McCullough Album</p>
        <h1 className="gallery-title">Our Favorite Moments</h1>
        <p className="gallery-intro">
          A scrapbook of the days worth remembering. Tap any photo to take a
          closer look.
        </p>
      </header>

      <div className="gallery-filters" role="group" aria-label="Filter photos">
        {categories.map((category) => (
          <button
            key={category}
            type="button"
            className={
              category === activeCategory
                ? 'filter-chip is-active'
                : 'filter-chip'
            }
            onClick={() => selectCategory(category)}
          >
            {category}
          </button>
        ))}
      </div>

      {visiblePhotos.length === 0 ? (
        <p className="gallery-empty">No photos here yet — check back soon!</p>
      ) : (
        <div className="photo-grid">
          {visiblePhotos.map((photo, index) => (
            <PhotoCard
              key={photo.id}
              photo={photo}
              onOpen={() => setLightboxIndex(index)}
            />
          ))}
        </div>
      )}

      {lightboxIndex !== null && (
        <Lightbox
          photo={visiblePhotos[lightboxIndex]}
          onClose={() => setLightboxIndex(null)}
          onPrev={showPrev}
          onNext={showNext}
        />
      )}
    </div>
  )
}
