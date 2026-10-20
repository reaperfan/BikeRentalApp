import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function validate({ username, email, password, confirm }) {
  const errors = {}
  if (username.trim().length < 3) errors.username = 'Legalább 3 karakter.'
  if (!EMAIL_RE.test(email.trim())) errors.email = 'Érvénytelen email cím.'
  if (password.length < 8) errors.password = 'Legalább 8 karakter.'
  if (confirm !== password) errors.confirm = 'A két jelszó nem egyezik.'
  return errors
}

export default function Register() {
  const { user, register } = useAuth()
  const navigate = useNavigate()

  const [form, setForm] = useState({ username: '', email: '', password: '', confirm: '' })
  const [touched, setTouched] = useState({})
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to="/" replace />

  const errors = validate(form)
  const fieldError = (name) => touched[name] && errors[name]

  const handleSubmit = async (e) => {
    e.preventDefault()
    setTouched({ username: true, email: true, password: true, confirm: true })
    if (Object.keys(errors).length > 0) return

    setError('')
    setSubmitting(true)
    try {
      await register(form.username.trim(), form.email.trim(), form.password)
      navigate('/login', { state: { registered: true } })
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  const field = (name, label, type, autoComplete) => (
    <label>
      {label}
      <input
        type={type}
        autoComplete={autoComplete}
        value={form[name]}
        onChange={(e) => setForm({ ...form, [name]: e.target.value })}
        onBlur={() => setTouched({ ...touched, [name]: true })}
        aria-invalid={!!fieldError(name)}
      />
      {fieldError(name) && <span className="field-error">{errors[name]}</span>}
    </label>
  )

  return (
    <main className="auth-layout page-width register-layout">
      <aside className="auth-showcase register-showcase">
        <span className="showcase-label">JÓ TÁRSASÁG. JÓ UTAK.</span>
        <div className="showcase-spark" aria-hidden="true">✳</div>
        <div className="showcase-message">
          <h1>Az első<br />tekerés<br />itt kezdődik.</h1>
          <p>Hozz létre egy fiókot pár pillanat alatt.</p>
        </div>
        <div className="showcase-bottom"><span>CSATLAKOZZ A KÖZÖSSÉGHEZ</span><span aria-hidden="true">↗</span></div>
      </aside>

      <form className="auth-card" onSubmit={handleSubmit} noValidate>
        <div className="form-heading">
          <span className="section-kicker">INDULJUNK</span>
          <h2>Regisztráció</h2>
          <p>Hozd létre a fiókodat, és indulhat a közös út.</p>
        </div>

        {error && <p className="alert error" role="alert">{error}</p>}

        {field('username', 'Felhasználónév', 'text', 'username')}
        {field('email', 'Email', 'email', 'email')}
        {field('password', 'Jelszó', 'password', 'new-password')}
        {field('confirm', 'Jelszó megerősítése', 'password', 'new-password')}

        <button className="button-primary form-submit" type="submit" disabled={submitting}>
          {submitting ? 'Regisztráció...' : 'Fiók létrehozása'} <span aria-hidden="true">→</span>
        </button>

        <p className="switch">
          Van már fiókod? <Link to="/login">Bejelentkezés</Link>
        </p>
      </form>
    </main>
  )
}
