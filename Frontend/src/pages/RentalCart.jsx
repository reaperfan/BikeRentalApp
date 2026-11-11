import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { createRental } from '../api/bikeService'
import { useAuth } from '../auth/AuthContext'
import { loadCart, removeFromCart } from '../utils/rentalCart'
import { daysBetween } from '../utils/rentalDates'
import '../styles/RentalCart.css'

export default function RentalCart() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const [items, setItems] = useState(loadCart)
  const [submitting, setSubmitting] = useState(false)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState(null)

  const total = items.reduce(
    (sum, item) => sum + daysBetween(item.startDate, item.endDate) * item.dailyPrice,
    0
  )

  function handleRemove(index) {
    removeFromCart(index)
    setItems(loadCart())
  }

  async function handleSubmit() {
    setError(null)
    setSubmitting(true)
    try {
      for (const item of loadCart()) {
        await createRental({
          bikeId: item.bikeId,
          customerName: user.username,
          customerEmail: user.email,
          startDate: item.startDate,
          endDate: item.endDate
        })
        removeFromCart(0)
        setItems(loadCart())
      }
      setSuccess(true)
      setTimeout(() => navigate('/catalog'), 2000)
    } catch (err) {
      setError(`A foglalás sikertelen: ${err.message}`)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="rental-cart-page">
      <div className="rental-cart">
        <h1>Kosár</h1>

        {success && (
          <div className="alert success">
            ✓ Foglalás sikeresen létrehozva! Átirányítás...
          </div>
        )}

        {error && <div className="alert error">{error}</div>}

        {items.length === 0 ? (
          <div className="cart-empty">
            <p>A kosár üres.</p>
            <Link to="/catalog" className="btn-back">Vissza a katalógushoz</Link>
          </div>
        ) : (
          <>
            <ul className="cart-list">
              {items.map((item, index) => {
                const days = daysBetween(item.startDate, item.endDate)
                return (
                  <li key={item.bikeId} className="cart-item">
                    <div className="cart-item-info">
                      <h3>{item.name}</h3>
                      <p>{item.startDate} – {item.endDate}</p>
                      <p>{days} nap × {item.dailyPrice.toLocaleString('hu-HU')} Ft/nap</p>
                    </div>
                    <div className="cart-item-side">
                      <strong>{(days * item.dailyPrice).toLocaleString('hu-HU')} Ft</strong>
                      <button
                        type="button"
                        className="btn-remove"
                        onClick={() => handleRemove(index)}
                        disabled={submitting}
                      >
                        Eltávolítás
                      </button>
                    </div>
                  </li>
                )
              })}
            </ul>

            <div className="cart-total">
              <p><strong>Összesen:</strong> {total.toLocaleString('hu-HU')} Ft</p>
            </div>

            <button
              type="button"
              className="btn-submit"
              onClick={handleSubmit}
              disabled={submitting || success}
            >
              {submitting ? 'Feldolgozás...' : 'Foglalás megerősítése'}
            </button>
          </>
        )}

        <Link to="/catalog" className="btn-back">← Vissza a katalógushoz</Link>
      </div>
    </div>
  )
}
