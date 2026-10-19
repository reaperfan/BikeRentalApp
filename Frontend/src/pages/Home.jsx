import { useAuth } from '../auth/AuthContext'

export default function Home() {
  const { user, logout } = useAuth()

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Szia, {user.username}!</h1>
        <p>{user.email}</p>
        <p>Szerepkör: {user.role}</p>
        <button onClick={logout}>Kijelentkezés</button>
      </div>
    </div>
  )
}
