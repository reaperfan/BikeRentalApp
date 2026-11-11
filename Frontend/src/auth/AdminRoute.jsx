import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

export default function AdminRoute({ children }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return <p className="center-text">Betöltés...</p>
  if (!user) return <Navigate to="/login" replace state={{ from: location }} />
  // Nem admin felhasználó a főoldalra kerül; a backend is ellenőriz (403)
  if (user.role !== 'Admin') return <Navigate to="/" replace />
  return children
}
