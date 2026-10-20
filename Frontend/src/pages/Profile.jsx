import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function Profile() {
  const { user } = useAuth()
  const createdAt = new Intl.DateTimeFormat('hu-HU', {
    dateStyle: 'long',
    timeStyle: 'short',
  }).format(new Date(user.createdAt))

  return (
    <main className="profile-page page-width">
      <div className="profile-heading">
        <div>
          <span className="section-kicker">FIÓKOD</span>
          <h1>Profil áttekintés</h1>
          <p>Itt találod a BikeRental-fiókodhoz tartozó adatokat.</p>
        </div>
        <Link className="button-secondary" to="/">← Vissza a főoldalra</Link>
      </div>

      <section className="profile-card" aria-label="Profiladatok">
        <div className="profile-banner">
          <div className="profile-avatar">{user.username?.[0]?.toUpperCase()}</div>
          <div className="profile-identity">
            <span className="profile-status"><span /> AKTÍV FIÓK</span>
            <h2>{user.username}</h2>
            <p>{user.email}</p>
          </div>
          <span className="profile-decoration" aria-hidden="true">✳</span>
        </div>

        <div className="profile-details">
          <div className="detail-item"><span>Felhasználónév</span><strong>{user.username}</strong></div>
          <div className="detail-item"><span>E-mail-cím</span><strong>{user.email}</strong></div>
          <div className="detail-item"><span>Szerepkör</span><strong className="role-pill">{user.role}</strong></div>
          <div className="detail-item"><span>Regisztráció dátuma</span><strong>{createdAt}</strong></div>
          <div className="detail-item detail-id"><span>Felhasználói azonosító</span><strong>{user.id}</strong></div>
        </div>
      </section>

      <div className="profile-tip"><span aria-hidden="true">✦</span><p>Ez a profil a fiókodhoz tartozó alapadatokat mutatja.</p></div>
    </main>
  )
}
