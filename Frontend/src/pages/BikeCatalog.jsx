import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getBikes, getRentals } from '../api/bikeService'
import { useAuth } from '../auth/AuthContext'
import BikeCard from '../components/BikeCard'
import DateRangeCalendar from '../components/DateRangeCalendar'
import { addToCart, loadCart } from '../utils/rentalCart'
import { addDays, getBookedDays, isRangeAvailable, toISODate } from '../utils/rentalDates'
import '../styles/BikeCatalog.css'
import '../styles/CatalogLayout.css'

const BIKE_TYPES = [
  { value: undefined, label: 'Összes típus' },
  { value: 0, label: 'MTB' },
  { value: 1, label: 'Trekking' },
  { value: 2, label: 'City' },
  { value: 3, label: 'EBike' },
  { value: 4, label: 'Gyerek' }
]

const minRentalDate = () => addDays(toISODate(new Date()), 1)

export default function BikeCatalog() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const [bikes, setBikes] = useState([])
  const [rentals, setRentals] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [selectedType, setSelectedType] = useState(undefined)
  const [selectedBikeId, setSelectedBikeId] = useState(null)
  const [range, setRange] = useState({ start: null, end: null })
  const [cartCount, setCartCount] = useState(() => loadCart().length)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        setError(null)
        const [bikeData, rentalData] = await Promise.all([
          getBikes({ type: selectedType }),
          getRentals()
        ])
        if (cancelled) return
        setBikes(bikeData || [])
        setRentals(rentalData || [])
      } catch (err) {
        if (!cancelled) setError(err.message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [selectedType])

  const selectedBike = bikes.find(bike => bike.id === selectedBikeId) ?? null
  const hasRange = !!range.start && !!range.end

  const bookedDays = useMemo(
    () => (selectedBike ? getBookedDays(rentals, selectedBike.id) : new Set()),
    [rentals, selectedBike]
  )

  const visibleBikes = useMemo(() => {
    if (!hasRange) return bikes
    return bikes.filter(bike => isRangeAvailable(rentals, bike.id, range.start, range.end))
  }, [bikes, rentals, range, hasRange])

  function handleSelectBike(bike) {
    setMessage(null)
    setSelectedBikeId(bike.id === selectedBikeId ? null : bike.id)
  }

  function handleAddToCart(bike) {
    if (!user) {
      navigate('/login')
      return
    }
    if (!hasRange) {
      setMessage('Először válassz kezdő és befejező napot a naptárban.')
      return
    }
    addToCart(bike, range.start, range.end)
    setCartCount(loadCart().length)
    setMessage(`${bike.name} hozzáadva a kosárhoz.`)
  }

  return (
    <div className="bike-catalog">
      <div className="catalog-container">
        <h1>Bicikli bérlés</h1>

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
            <button type="button" className="btn-cart" onClick={() => navigate('/cart')}>
              Kosár ({cartCount})
            </button>
          </div>
        </div>

        {message && <div className="success-message">{message}</div>}
        {error && <div className="error-message">Hiba: {error}</div>}

        <div className="catalog-layout">
          <div className="catalog-main">
            {loading && <div className="loading">Betöltés...</div>}

            {!loading && visibleBikes.length === 0 && (
              <div className="no-bikes">Nem találhatók biciklik a kiválasztott szűrők és dátum alapján.</div>
            )}

            {!loading && visibleBikes.length > 0 && (
              <div className="bikes-grid">
                {visibleBikes.map(bike => (
                  <div
                    key={bike.id}
                    onClick={() => handleSelectBike(bike)}
                    className={bike.id === selectedBikeId ? 'bike-card-wrapper selected' : 'bike-card-wrapper'}
                  >
                    <BikeCard
                      bike={bike}
                      canAddToCart={hasRange}
                      onAddToCart={() => handleAddToCart(bike)}
                    />
                  </div>
                ))}
              </div>
            )}
          </div>

          <aside className="catalog-calendar">
            <h2>Foglalási időszak</h2>
            {selectedBike && <p className="calendar-note">Bicikli: <strong>{selectedBike.name}</strong></p>}
            <DateRangeCalendar
              start={range.start}
              end={range.end}
              bookedDays={bookedDays}
              minDate={minRentalDate()}
              onChange={setRange}
            />
          </aside>
        </div>
      </div>
    </div>
  )
}
