import { useEffect, useState, useCallback } from 'react'
import { getBikes } from '../api/bikeService'
import BikeCard from '../components/BikeCard'
import '../styles/BikeCatalog.css'

const BIKE_TYPES = [
  { value: undefined, label: 'Összes típus' },
  { value: 0, label: 'MTB' },
  { value: 1, label: 'Trekking' },
  { value: 2, label: 'City' },
  { value: 3, label: 'EBike' },
  { value: 4, label: 'Gyerek' }
]

export default function BikeCatalog() {
  const [bikes, setBikes] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [selectedType, setSelectedType] = useState(undefined)
  const [onlyAvailable, setOnlyAvailable] = useState(false)

  const loadBikes = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await getBikes({
        type: selectedType,
        available: onlyAvailable ? true : undefined
      })
      setBikes(data || [])
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }, [selectedType, onlyAvailable])

  useEffect(() => {
    loadBikes()
  }, [loadBikes])

  return (
    <div className="bike-catalog">
      <div className="catalog-container">
        <h1>Bicikli Katalógus</h1>

        <div className="filters">
          <div className="filter-group">
            <label htmlFor="type-filter">Típus:</label>
            <select 
              id="type-filter"
              value={selectedType ?? ''}
              onChange={(e) => setSelectedType(e.target.value === '' ? undefined : parseInt(e.target.value))}
            >
              {BIKE_TYPES.map(type => (
                <option key={type.label} value={type.value ?? ''}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="available-filter">
              <input
                id="available-filter"
                type="checkbox"
                checked={onlyAvailable}
                onChange={(e) => setOnlyAvailable(e.target.checked)}
              />
              Csak elérhető biciklil
            </label>
          </div>
        </div>

        {error && <div className="error-message">Hiba: {error}</div>}

        {loading && <div className="loading">Betöltés...</div>}

        {!loading && bikes.length === 0 && (
          <div className="no-bikes">Nem találhatók biciklil a kiválasztott szűrők alapján.</div>
        )}

        {!loading && bikes.length > 0 && (
          <div className="bikes-grid">
            {bikes.map(bike => (
              <BikeCard key={bike.id} bike={bike} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
