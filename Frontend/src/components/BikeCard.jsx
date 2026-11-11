import { useState } from 'react'

export default function BikeCard({ bike, canAddToCart, onAddToCart }) {
  const [imageError, setImageError] = useState(false)

  const bikeTypeNames = {
    0: 'MTB',
    1: 'Trekking',
    2: 'City',
    3: 'E-Bike',
    4: 'Gyerek'
  }

  function handleAddClick(e) {
    e.stopPropagation()
    onAddToCart()
  }

  return (
    <div className="bike-card">
      <div className="bike-image">
        {!imageError ? (
          <img
            src={bike.imageUrl}
            alt={bike.name}
            onError={() => setImageError(true)}
          />
        ) : (
          <div className="image-placeholder">
            <span>📷</span>
            <p>Kép nem elérhető</p>
          </div>
        )}
        {!bike.isAvailable && (
          <div className="unavailable-badge">Nem elérhető</div>
        )}
      </div>

      <div className="bike-info">
        <h3>{bike.name}</h3>
        <p className="bike-type">{bikeTypeNames[bike.type] || 'Ismeretlen'}</p>
        <p className="bike-price">{bike.dailyPrice.toLocaleString('hu-HU')} Ft/nap</p>

        {bike.isAvailable ? (
          <button
            type="button"
            className="btn-rental"
            onClick={handleAddClick}
            disabled={!canAddToCart}
          >
            Kosárba
          </button>
        ) : (
          <button type="button" className="btn-unavailable" disabled>
            Nem elérhető
          </button>
        )}
      </div>
    </div>
  )
}
