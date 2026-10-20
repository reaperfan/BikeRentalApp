import { useAuth } from '../auth/AuthContext'
import { Link } from 'react-router-dom'

export default function Home() {
  const { user } = useAuth()

  return (
    <main className="home-page page-width">
      <section className="hero-panel">
        <div className="hero-copy">
          <span className="eyebrow"><span className="eyebrow-dot" /> JÓ ÚTON VAGY</span>
          <h1>Szia, {user.username}!<br /><span>Indulhat a kaland.</span></h1>
          <p>A város más arcát mutatja két keréken. Örülünk, hogy velünk vagy — a profilod már készen áll.</p>
          <Link className="button-primary" to="/profile">Megnézem a profilom <span aria-hidden="true">→</span></Link>
          <div className="hero-note"><span className="avatar-dot">{user.username?.[0]?.toUpperCase()}</span><span>Belépve mint <strong>{user.email}</strong></span></div>
        </div>

        <div className="hero-art" aria-hidden="true">
          <div className="art-sun" />
          <div className="art-orbit orbit-one" />
          <div className="art-orbit orbit-two" />
          <svg className="bike-illustration" viewBox="0 0 440 300" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="104" cy="211" r="67" stroke="currentColor" strokeWidth="8" />
            <circle cx="332" cy="211" r="67" stroke="currentColor" strokeWidth="8" />
            <path d="M104 211L176 105L239 211H104ZM176 105L226 105M239 211L277 112L332 211M264 112H294M156 89H190M176 105L188 82" stroke="currentColor" strokeWidth="9" strokeLinecap="round" strokeLinejoin="round" />
            <circle cx="239" cy="211" r="8" fill="currentColor" />
          </svg>
          <span className="art-label label-top">SZABADON</span>
          <span className="art-label label-bottom">A SAJÁT TEMPÓDBAN</span>
          <span className="art-star star-one">✳</span>
          <span className="art-star star-two">✦</span>
        </div>
      </section>

      <section className="welcome-row">
        <div>
          <span className="section-kicker">BIKERENTAL KLUB</span>
          <h2>Jó, hogy itt vagy.</h2>
          <p>A fiókodat bármikor megnézheted és kezelheted.</p>
        </div>
        <Link className="text-link" to="/profile">Fiókbeállítások <span aria-hidden="true">↗</span></Link>
      </section>

      <section className="feature-grid" aria-label="A BikeRental előnyei">
        <article className="feature-card feature-lime">
          <span className="feature-icon" aria-hidden="true">↗</span>
          <span className="feature-number">01</span>
          <h3>Indulj a saját ritmusodban</h3>
          <p>Találd meg a városi utakhoz illő tempót — a kaland ott kezdődik, ahol te szeretnéd.</p>
        </article>
        <article className="feature-card feature-lilac">
          <span className="feature-icon" aria-hidden="true">◎</span>
          <span className="feature-number">02</span>
          <h3>Minden egy helyen</h3>
          <p>A profiloldaladon egy pillantással eléred a fiókodhoz tartozó adatokat.</p>
        </article>
        <article className="feature-card feature-peach">
          <span className="feature-icon" aria-hidden="true">✳</span>
          <span className="feature-number">03</span>
          <h3>Élvezd az utat</h3>
          <p>Kevesebb tervezgetés, több friss levegő és új nézőpont a mindennapokban.</p>
        </article>
      </section>

      <footer className="site-footer">
        <span>BIKERENTAL</span>
        <span>Jó utat, {user.username}!</span>
      </footer>
    </main>
  )
}
