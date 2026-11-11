import { getStats } from '../../api/adminService'
import useAdminResource from '../../hooks/useAdminResource'
import Failure from './Failure'
import { money } from './format'

export default function AdminOverview({ onNavigate }) {
  const [stats, reload] = useAdminResource(getStats)

  if (stats.loading) return <p role="status">Összesítő betöltése…</p>
  if (stats.error) return <Failure message={stats.error} retry={reload} />

  const s = stats.data
  const cards = [
    { label: 'Biciklik', value: s.bikeCount, note: `${s.availableBikeCount} elérhető`, tab: 'bikes', tone: 'lime' },
    { label: 'Aktív kölcsönzés', value: s.activeRentalCount, note: `${s.upcomingRentalCount} közelgő · ${s.rentalCount} összesen`, tab: 'rentals', tone: 'lilac' },
    { label: 'Bevétel', value: money(s.totalRevenue), note: 'Összes kölcsönzés alapján', tab: 'rentals', tone: 'peach' },
    { label: 'Felhasználók', value: s.userCount, note: `${s.adminCount} admin`, tab: 'users', tone: 'paper' },
    { label: 'Függő szerviz', value: s.pendingServiceAppointmentCount, note: 'Visszaigazolásra vár', tab: 'service', tone: s.pendingServiceAppointmentCount > 0 ? 'warn' : 'paper' },
    { label: 'Értékelések', value: s.reviewCount, note: s.averageRating == null ? 'Még nincs értékelés' : `${s.averageRating.toFixed(1)} / 5 átlag`, tone: 'paper' }
  ]

  return (
    <>
      <div className="admin-panel-heading">
        <h2>Áttekintés</h2>
        <button type="button" className="button-secondary" onClick={reload}>Frissítés ↻</button>
      </div>
      <div className="admin-stats">
        {cards.map((card) => {
          const content = <><span>{card.label}</span><strong>{card.value}</strong><small>{card.note}</small></>
          return card.tab
            ? <button key={card.label} type="button" className={`admin-stat tone-${card.tone}`} onClick={() => onNavigate(card.tab)}>{content}</button>
            : <div key={card.label} className={`admin-stat tone-${card.tone}`}>{content}</div>
        })}
      </div>
    </>
  )
}
