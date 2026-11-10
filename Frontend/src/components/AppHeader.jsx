import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function AppHeader() {
  const { user, logout } = useAuth()

  return (
    <header className="site-header">
      <Link className="brand" to={user ? '/' : '/login'} aria-label="BikeRental főoldal">
        <span className="brand-mark" aria-hidden="true">B</span>
        <span>bike<span className="brand-accent">rental</span></span>
      </Link>

      <nav className="main-nav" aria-label="Fő navigáció">
        <NavLink to="/ertekelesek" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>Értékelések</NavLink>
        {user ? (
          <>
            <NavLink to="/" end className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>Főoldal</NavLink>
            <NavLink to="/catalog" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>Bérlés</NavLink>
            <NavLink to="/profile" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>Profil</NavLink>
            <NavLink to="/szerviz" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>Szerviz</NavLink>
            <button className="nav-logout" type="button" onClick={logout}>Kijelentkezés</button>
          </>
        ) : (
          <>
            <NavLink to="/login" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>Bejelentkezés</NavLink>
            <Link className="nav-cta" to="/register">Fiók létrehozása <span aria-hidden="true">↗</span></Link>
          </>
        )}
      </nav>
    </header>
  )
}
