import { useState } from 'react'
import { getUsers, updateUserRole } from '../../api/adminService'
import { useAuth } from '../../auth/AuthContext'
import useAdminResource from '../../hooks/useAdminResource'
import Failure from './Failure'
import { formatDate } from './format'

export default function AdminUsers() {
  const { user: me } = useAuth()
  const [users, reload] = useAdminResource(getUsers)
  const [search, setSearch] = useState('')
  const [busyId, setBusyId] = useState(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const term = search.trim().toLowerCase()
  const visible = (users.data ?? []).filter((u) =>
    !term || u.username.toLowerCase().includes(term) || u.email.toLowerCase().includes(term))

  async function changeRole(user) {
    const role = user.role === 'Admin' ? 'Customer' : 'Admin'
    setBusyId(user.id)
    setError('')
    setMessage('')
    try {
      await updateUserRole(user.id, role)
      setMessage(`${user.username} szerepköre: ${role}. Következő bejelentkezésekor lép életbe.`)
      reload()
    } catch (err) {
      setError(err.message)
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      {message && <p className="alert success" role="status">{message}</p>}
      {error && <p className="alert error" role="alert">{error}</p>}

      <div className="admin-panel-heading">
        <h2>Felhasználók {users.data && <small>({users.data.length})</small>}</h2>
        <div className="admin-heading-tools">
          <input type="search" aria-label="Keresés név vagy e-mail alapján" placeholder="Keresés…" value={search} onChange={(e) => setSearch(e.target.value)} />
          <button type="button" className="button-secondary" onClick={reload}>Frissítés ↻</button>
        </div>
      </div>
      {users.loading && <p role="status">Felhasználók betöltése…</p>}
      {users.error && <Failure message={users.error} retry={reload} />}
      {users.data && visible.length === 0 && <p className="admin-empty">Nincs találat.</p>}
      {visible.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead><tr><th>#</th><th>Felhasználó</th><th>Regisztrált</th><th>Szerepkör</th><th><span className="visually-hidden">Műveletek</span></th></tr></thead>
            <tbody>
              {visible.map((u) => (
                <tr key={u.id}>
                  <td>{u.id}</td>
                  <td><strong>{u.username}{u.id === me.id && ' (te)'}</strong><small>{u.email}</small></td>
                  <td>{formatDate(u.createdAt)}</td>
                  <td><span className={`admin-badge ${u.role === 'Admin' ? 'admin' : 'customer'}`}>{u.role}</span></td>
                  <td className="admin-actions">
                    {u.id !== me.id && (
                      <button type="button" className="button-secondary" disabled={busyId !== null} onClick={() => changeRole(u)}>
                        {busyId === u.id ? 'Mentés…' : u.role === 'Admin' ? 'Admin jog elvétele' : 'Admin jog megadása'}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
