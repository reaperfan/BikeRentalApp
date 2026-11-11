import { Link, useSearchParams } from 'react-router-dom'
import AdminOverview from './admin/AdminOverview'
import AdminBikes from './admin/AdminBikes'
import AdminRentals from './admin/AdminRentals'
import AdminUsers from './admin/AdminUsers'
import AdminService from './admin/AdminService'
import './Szerviz.css'
import '../styles/Admin.css'

const tabs = [
  { id: 'overview', label: 'Áttekintés', Component: AdminOverview },
  { id: 'bikes', label: 'Biciklik', Component: AdminBikes },
  { id: 'rentals', label: 'Kölcsönzések', Component: AdminRentals },
  { id: 'users', label: 'Felhasználók', Component: AdminUsers },
  { id: 'service', label: 'Szerviz', Component: AdminService }
]

export default function Admin() {
  const [params, setParams] = useSearchParams()
  const active = tabs.find((t) => t.id === params.get('tab')) ?? tabs[0]
  const open = (id) => setParams(id === tabs[0].id ? {} : { tab: id })

  return (
    <main className="admin-page page-width">
      <div className="profile-heading">
        <div>
          <span className="section-kicker">VEZÉRLŐPULT</span>
          <h1>Admin</h1>
          <p>Biciklik, kölcsönzések, felhasználók és szervizfoglalások kezelése.</p>
        </div>
        <Link className="button-secondary" to="/ertekelesek">Értékelések moderálása →</Link>
      </div>

      <nav className="admin-tabs" role="tablist" aria-label="Admin részek">
        {tabs.map((tab) => (
          <button key={tab.id} type="button" role="tab" id={`admin-tab-${tab.id}`} aria-selected={tab.id === active.id}
            aria-controls="admin-panel" className={tab.id === active.id ? 'active' : ''} onClick={() => open(tab.id)}>
            {tab.label}
          </button>
        ))}
      </nav>

      <section className="admin-panel" id="admin-panel" role="tabpanel" aria-labelledby={`admin-tab-${active.id}`}>
        <active.Component onNavigate={open} />
      </section>
    </main>
  )
}
