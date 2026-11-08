import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getBikeById, createRental } from '../api/bikeService'
import '../styles/BikeRental.css'

export default function BikeRental() {
  const { bikeId } = useParams()
  const navigate = useNavigate()

  const [bike, setBike] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(false)

  const [formData, setFormData] = useState({
    customerName: '',
    customerEmail: '',
    startDate: '',
    endDate: ''
  })

  const [submitting, setSubmitting] = useState(false)
  const [validationError, setValidationError] = useState(null)

  const loadBike = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await getBikeById(bikeId)
      if (!data) {
        setError('Bicikli nem található.')
        return
      }
      setBike(data)
      // Legkorábbi kezdési dátum: holnap
      const tomorrow = new Date()
      tomorrow.setDate(tomorrow.getDate() + 1)
      const minDate = tomorrow.toISOString().split('T')[0]
      setFormData(prev => ({ ...prev, startDate: minDate, endDate: minDate }))
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }, [bikeId])

  useEffect(() => {
    loadBike()
  }, [loadBike])

  function handleChange(e) {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: value }))
    setValidationError(null)
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setValidationError(null)

    // Validáció
    if (!formData.customerName.trim()) {
      setValidationError('Kérlek add meg a nevedet.')
      return
    }
    if (!formData.customerEmail.trim()) {
      setValidationError('Kérlek add meg az email címedet.')
      return
    }
    if (!formData.startDate) {
      setValidationError('Kérlek válassz kezdési dátumot.')
      return
    }
    if (!formData.endDate) {
      setValidationError('Kérlek válassz befejezési dátumot.')
      return
    }

    const start = new Date(formData.startDate)
    const end = new Date(formData.endDate)

    if (end <= start) {
      setValidationError('A befejezési dátum később kell legyen, mint a kezdési dátum.')
      return
    }

    try {
      setSubmitting(true)
      await createRental({
        bikeId: parseInt(bikeId),
        customerName: formData.customerName,
        customerEmail: formData.customerEmail,
        startDate: formData.startDate,
        endDate: formData.endDate
      })
      setSuccess(true)
      setTimeout(() => {
        navigate('/catalog')
      }, 2000)
    } catch (err) {
      setValidationError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <div className="rental-container"><div className="loading">Bicikli adatainak betöltése...</div></div>
  }

  if (error) {
    return (
      <div className="rental-container">
        <div className="error-message">{error}</div>
        <button className="btn-back" onClick={() => navigate('/catalog')}>
          Vissza a katalógushoz
        </button>
      </div>
    )
  }

  if (!bike) {
    return (
      <div className="rental-container">
        <div className="error-message">Bicikli nem található.</div>
        <button className="btn-back" onClick={() => navigate('/catalog')}>
          Vissza a katalógushoz
        </button>
      </div>
    )
  }

  const bikeTypeNames = {
    0: 'MTB',
    1: 'Trekking',
    2: 'City',
    3: 'E-Bike',
    4: 'Gyerek'
  }

  return (
    <div className="rental-container">
      <div className="rental-content">
        <button className="btn-back" onClick={() => navigate('/catalog')}>
          ← Vissza a katalógushoz
        </button>

        <div className="rental-wrapper">
          <div className="bike-details">
            <img src={bike.imageUrl} alt={bike.name} onError={(e) => {
              e.target.src = '/images/placeholder.png'
            }} />
            <div className="details-info">
              <h1>{bike.name}</h1>
              <p className="type-label">{bikeTypeNames[bike.type] || 'Ismeretlen típus'}</p>
              <p className="price-label">
                <strong>{bike.dailyPrice.toLocaleString('hu-HU')} Ft/nap</strong>
              </p>
              <p className={`availability ${bike.isAvailable ? 'available' : 'unavailable'}`}>
                {bike.isAvailable ? '✓ Elérhető' : '✗ Nem elérhető'}
              </p>
            </div>
          </div>

          <form className="rental-form" onSubmit={handleSubmit}>
            <h2>Foglalás</h2>

            {success && (
              <div className="success-message">
                ✓ Foglalás sikeresen létrehozva! Átirányítás...
              </div>
            )}

            {validationError && (
              <div className="error-message">{validationError}</div>
            )}

            <div className="form-group">
              <label htmlFor="customerName">Teljes név *</label>
              <input
                id="customerName"
                type="text"
                name="customerName"
                value={formData.customerName}
                onChange={handleChange}
                placeholder="pl. Nagy János"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="customerEmail">Email cím *</label>
              <input
                id="customerEmail"
                type="email"
                name="customerEmail"
                value={formData.customerEmail}
                onChange={handleChange}
                placeholder="pl. nagy.janos@example.com"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="startDate">Kezdési dátum *</label>
              <input
                id="startDate"
                type="date"
                name="startDate"
                value={formData.startDate}
                onChange={handleChange}
                min={new Date().toISOString().split('T')[0]}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="endDate">Befejezési dátum *</label>
              <input
                id="endDate"
                type="date"
                name="endDate"
                value={formData.endDate}
                onChange={handleChange}
                min={formData.startDate || new Date().toISOString().split('T')[0]}
                required
              />
            </div>

            {formData.startDate && formData.endDate && (() => {
              const startDate = new Date(formData.startDate)
              const endDate = new Date(formData.endDate)
              const days = Math.ceil((endDate - startDate) / (1000 * 60 * 60 * 24))
              const total = days * bike.dailyPrice
              return (
                <div className="rental-summary">
                  <p>
                    <strong>Kölcsönzési napok:</strong> {days} nap
                  </p>
                  <p>
                    <strong>Összesen:</strong> {total.toLocaleString('hu-HU')} Ft
                  </p>
                </div>
              )
            })()}

            <button
              type="submit"
              className="btn-submit"
              disabled={submitting || !bike.isAvailable}
            >
              {submitting ? 'Feldolgozás...' : 'Foglalás megerősítése'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
