import { useCallback, useState } from 'react'
import { getServiceAppointments, getUsers, updateServiceStatus } from '../../api/adminService'
import useAdminResource from '../../hooks/useAdminResource'
import Failure from './Failure'
import { addDays, formatDate, isoDay } from './format'

const labels = { Pending: 'Visszaigazolásra vár', Confirmed: 'Visszaigazolva', Completed: 'Elkészült', Cancelled: 'Lemondva' }
// A backend által engedélyezett státuszváltások
const transitions = {
  Pending: [['Confirmed', 'Visszaigazolás'], ['Cancelled', 'Lemondás']],
  Confirmed: [['Completed', 'Elkészült'], ['Cancelled', 'Lemondás']]
}

export default function AdminService() {
  const [range, setRange] = useState(() => ({ from: isoDay(), to: addDays(isoDay(), 30) }))
  const [statusFilter, setStatusFilter] = useState('')
  const { from, to } = range
  const load = useCallback(() => getServiceAppointments(from, to), [from, to])
  const [items, reload] = useAdminResource(load)
  const [users] = useAdminResource(getUsers)
  const [busyId, setBusyId] = useState(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const names = new Map((users.data ?? []).map((u) => [u.id, u]))
  const visible = (items.data ?? []).filter((i) => !statusFilter || i.appointment.status === statusFilter)

  async function change(id, status) {
    setBusyId(id)
    setError('')
    setMessage('')
    try {
      await updateServiceStatus(id, status)
      setMessage(`A #${id} foglalás új státusza: ${labels[status]}.`)
      reload()
    } catch (err) {
      setError(err.message)
      reload()
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      {message && <p className="alert success" role="status">{message}</p>}
      {error && <p className="alert error" role="alert">{error}</p>}

      <div className="admin-filters">
        <label>Ettől<input type="date" required value={from} max={to} onChange={(e) => e.target.value && setRange({ ...range, from: e.target.value })} /></label>
        <label>Eddig<input type="date" required value={to} min={from} onChange={(e) => e.target.value && setRange({ ...range, to: e.target.value })} /></label>
        <label>Státusz
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">Összes</option>
            {Object.entries(labels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
      </div>

      <div className="admin-panel-heading">
        <h2>Szervizfoglalások {items.data && <small>({visible.length})</small>}</h2>
        <button type="button" className="button-secondary" disabled={busyId !== null} onClick={reload}>Frissítés ↻</button>
      </div>
      {items.loading && <p role="status">Foglalások betöltése…</p>}
      {items.error && <Failure message={items.error} retry={reload} />}
      {items.data && visible.length === 0 && <p className="admin-empty">Ebben az időszakban nincs szervizfoglalás.</p>}
      {visible.map(({ userId, appointment: a }) => (
        <article className="appointment-card" key={a.id}>
          <div className="appointment-details">
            <span className={`service-status status-${a.status}`}>{labels[a.status] ?? a.status}</span>
            <h3>{formatDate(a.appointmentDate, { dateStyle: 'long', timeStyle: 'short' })}</h3>
            <p><strong>{a.bikeType}</strong> · {a.serviceType}</p>
            <p>{names.get(userId) ? `${names.get(userId).username} · ${names.get(userId).email}` : `Felhasználó #${userId}`}</p>
            {a.problemDescription && <p className="appointment-description">{a.problemDescription}</p>}
          </div>
          {transitions[a.status] && (
            <div className="appointment-actions">
              {transitions[a.status].map(([status, label]) => (
                <button key={status} type="button" className={status === 'Cancelled' ? 'service-danger' : 'button-secondary'}
                  disabled={busyId !== null} onClick={() => change(a.id, status)}>{busyId === a.id ? 'Mentés…' : label}</button>
              ))}
            </div>
          )}
        </article>
      ))}
    </>
  )
}
