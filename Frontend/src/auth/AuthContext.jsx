import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import { api, getToken, setToken } from '../api/client'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(() => !!getToken())

  const logout = useCallback(() => {
    setToken(null)
    setUser(null)
  }, [])

  const loadUser = useCallback(async () => {
    try {
      setUser(await api('/auth/me'))
    } catch {
      // lejárt vagy érvénytelen token
      logout()
    } finally {
      setLoading(false)
    }
  }, [logout])

  // Oldalbetöltéskor a mentett tokenből visszaállítja a usert
  useEffect(() => {
    if (getToken()) loadUser()
  }, [loadUser])

  const login = async (email, password) => {
    const res = await api('/auth/login', { method: 'POST', body: { email, password }, auth: false })
    setToken(res.accessToken)
    await loadUser()
  }

  const register = (username, email, password) =>
    api('/auth/register', { method: 'POST', body: { username, email, password }, auth: false })

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}
