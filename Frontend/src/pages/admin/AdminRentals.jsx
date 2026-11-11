import { useCallback, useState } from 'react'
import { deleteAdminRental, getAdminRentals } from '../../api/adminService'
import { getBikes } from '../../api/bikeService'
import useAdminResource from '../../hooks/useAdminResource'
import Failure from './Failure'
import { addDays, formatDate, money } from './format'

export default function AdminRentals() {
  const [filters, setFilters] = useState({ bikeId: '', from: '', to: '' })
  const { bikeId, from } = filters
  // A záró nap is beletartozzon a szűrésbe
  const to = filters.to && addDays(filters.to, 1)
  const load = useCallback(() => getAdminRentals({ bikeId, from, to }), [bikeId, from, to])
  const [rentals, reload] = useAdminResource(load)
  const [bikes] = useAdminResource(useCallback(() => getBikes(), []))
  const [deleteId, setDeleteId] = useState(null)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const total = (rentals.data ?? []).reduce((sum, r) => sum + r.totalPrice, 0)
  const [now] = useState(() => Date.now())
  const status = (r) => new Date(r.endDate).getTime() <= now ? ['done', 'Lezárult']
    : new Date(r.startDate).getTime() <= now ? ['active', 'Folyamatban'] : ['upcoming', 'Közelgő']

  async function remove(id) {
    setBusy(true)
    setError('')
    setMessage('')
    try {
      await deleteAdminRental(id)
      setMessage(`A #${id} kölcsönzés törölve.`)
      reload()
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
      setDeleteId(null)
    }
  }

  return (
    <>
      {message && <p className="alert success" role="status">{message}</p>}
      {error && <p className="alert error" role="alert">{error}</p>}

      <div className="admin-filters">
        <label>Bicikli
          <select value={filters.bikeId} onChange={(e) => setFilters({ ...filters, bikeId: e.target.value })}>
            <option value="">Összes</option>
            {(bikes.data ?? []).map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </label>
        <label>Ettől<input type="date" value={filters.from} max={filters.to || undefined} onChange={(e) => setFilters({ ...filters, from: e.target.value })} /></label>
        <label>Eddig<input type="date" value={filters.to} min={filters.from || undefined} onChange={(e) => setFilters({ ...filters, to: e.target.value })} /></label>
        <button type="button" className="button-secondary" onClick={() => setFilters({ bikeId: '', from: '', to: '' })}>Szűrés törlése</button>
      </div>

      <div className="admin-panel-heading">
        <h2>Kölcsönzések {rentals.data && <small>({rentals.data.length} db · {money(total)})</small>}</h2>
        <button type="button" className="button-secondary" disabled={busy} onClick={reload}>Frissítés ↻</button>
      </div>
      {rentals.loading && <p role="status">Kölcsönzések betöltése…</p>}
      {rentals.error && <Failure message={rentals.error} retry={reload} />}
      {rentals.data?.length === 0 && <p className="admin-empty">Nincs a szűrésnek megfelelő kölcsönzés.</p>}
      {rentals.data?.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead><tr><th>#</th><th>Bicikli</th><th>Ügyfél</th><th>Időszak</th><th>Összeg</th><th>Állapot</th><th><span className="visually-hidden">Műveletek</span></th></tr></thead>
            <tbody>
              {rentals.data.map((r) => {
                const [tone, label] = status(r)
                return (
                  <tr key={r.id}>
                    <td>{r.id}</td>
                    <td><strong>{r.bikeName}</strong></td>
                    <td><strong>{r.customerName}</strong><small>{r.customerEmail}</small></td>
                    <td>{formatDate(r.startDate)} – {formatDate(r.endDate)}<small>{r.days} nap</small></td>
                    <td>{money(r.totalPrice)}</td>
                    <td><span className={`admin-badge ${tone}`}>{label}</span></td>
                    <td className="admin-actions">
                      {deleteId === r.id ? (
                        <>
                          <span>Biztosan törlöd?</span>
                          <button type="button" className="service-danger" disabled={busy} onClick={() => remove(r.id)}>Igen, törlöm</button>
                          <button type="button" className="button-secondary" disabled={busy} onClick={() => setDeleteId(null)}>Mégsem</button>
                        </>
                      ) : (
                        <button type="button" className="service-danger" disabled={busy} onClick={() => setDeleteId(r.id)}>Törlés</button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
