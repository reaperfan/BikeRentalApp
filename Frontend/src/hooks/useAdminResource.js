import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'

// Betölti az admin adatot; a load függvény legyen stabil (useCallback),
// a reload() újrakéri. Lejárt tokennél (401) kijelentkeztet.
export default function useAdminResource(load) {
  const { logout } = useAuth()
  const [state, setState] = useState({ loading: true, data: null, error: '' })
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    let active = true
    load()
      .then((data) => { if (active) setState({ loading: false, data, error: '' }) })
      .catch((err) => {
        if (!active) return
        if (err.status === 401) logout()
        setState({ loading: false, data: null, error: err.message })
      })
    return () => { active = false }
  }, [load, revision, logout])

  const reload = useCallback(() => setRevision((n) => n + 1), [])
  return [state, reload]
}
