import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'

// Egy korábbi szűrésre vagy felhasználóra érkező válasz nem írhatja felül az újat.
export function useReviewRequest(path, revision, auth = false) {
  const { logout } = useAuth()
  const [result, setResult] = useState(null)
  const key = `${path}/${revision}/${auth}`
  useEffect(() => {
    let active = true
    api(path, { auth }).then((data) => { if (active) setResult({ key, data }) })
      .catch((error) => {
        if (!active) return
        if (auth && error.status === 401) logout()
        setResult({ key, error: error.message })
      })
    return () => { active = false }
  }, [path, key, auth, logout])
  return result?.key === key ? { ...result, loading: false } : { loading: true }
}
