import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function Login() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const redirectTo = location.state?.from?.pathname ?? '/'
  const justRegistered = location.state?.registered

  if (user) return <Navigate to={redirectTo} replace />

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await login(email.trim(), password)
      navigate(redirectTo, { replace: true })
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="auth-layout page-width">
      <aside className="auth-showcase login-showcase">
        <span className="showcase-label">KÉT KERÉK. EZER LEHETŐSÉG.</span>
        <div className="showcase-spark" aria-hidden="true">✳</div>
        <div className="showcase-message">
          <h1>A város<br />vár rád.</h1>
          <p>Lépj be, és folytasd ott, ahol abbahagytad.</p>
        </div>
        <div className="showcase-bottom"><span>TEKERJ A SAJÁT UTADON</span><span aria-hidden="true">↗</span></div>
      </aside>

      <form className="auth-card" onSubmit={handleSubmit} noValidate>
        <div className="form-heading">
          <span className="section-kicker">ÜDV ÚJRA</span>
          <h2>Bejelentkezés</h2>
          <p>Jó látni! Add meg az adataidat a folytatáshoz.</p>
        </div>

        {justRegistered && <p className="alert success">Sikeres regisztráció! Most már bejelentkezhetsz.</p>}
        {error && <p className="alert error" role="alert">{error}</p>}

        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoFocus
          />
        </label>

        <label>
          Jelszó
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>

        <button className="button-primary form-submit" type="submit" disabled={submitting || !email || !password}>
          {submitting ? 'Bejelentkezés...' : 'Bejelentkezés'} <span aria-hidden="true">→</span>
        </button>

        <p className="switch">
          Nincs még fiókod? <Link to="/register">Regisztráció</Link>
        </p>
      </form>
    </main>
  )
}
