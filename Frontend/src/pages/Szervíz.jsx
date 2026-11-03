import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import './Szerviz.css'

const zone = 'Europe/Budapest'
const labels = { Pending: 'Visszaigazolásra vár', Confirmed: 'Visszaigazolva', Completed: 'Elkészült', Cancelled: 'Lemondva' }
const empty = { bikeType: '', serviceType: '', problemDescription: '' }
const format = (value, options) => new Intl.DateTimeFormat('hu-HU', { timeZone: zone, ...options }).format(new Date(value))
function dayOf(value) {
  const parts = new Intl.DateTimeFormat('en', { timeZone: zone, year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date(value))
  return ['year', 'month', 'day'].map((type) => parts.find((p) => p.type === type).value).join('-')
}
function addDays(day, count) {
  const date = new Date(`${day}T12:00:00Z`)
  date.setUTCDate(date.getUTCDate() + count)
  return date.toISOString().slice(0, 10)
}
function shiftMonth(month, count) {
  const date = new Date(`${month}-01T12:00:00Z`)
  date.setUTCMonth(date.getUTCMonth() + count)
  return date.toISOString().slice(0, 7)
}

export default function Service() {
  const { logout } = useAuth()
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 30_000)
    return () => clearInterval(timer)
  }, [])
  const today = dayOf(now)
  const horizon = addDays(today, 90)
  const [month, setMonth] = useState(today.slice(0, 7))
  const [day, setDay] = useState(today)
  const [slot, setSlot] = useState('')
  const [form, setForm] = useState(empty)
  const [editing, setEditing] = useState(null)
  const [cancelId, setCancelId] = useState(null)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [revision, setRevision] = useState(0)
  const [calendar, setCalendar] = useState(null)
  const [bookings, setBookings] = useState(null)
  const monthEnd = addDays(`${shiftMonth(month, 1)}-01`, -1)
  const from = `${month}-01` < today ? today : `${month}-01`
  const to = monthEnd > horizon ? horizon : monthEnd
  const key = `${from}/${to}/${revision}`

  useEffect(() => {
    let active = true
    api(`/service-appointments/availability?from=${from}&to=${to}`)
      .then((data) => { if (active) setCalendar({ key, data }) })
      .catch((err) => {
        if (!active) return
        if (err.status === 401) logout()
        setCalendar({ key, error: err.message })
      })
    return () => { active = false }
  }, [from, to, key, logout])
  useEffect(() => {
    let active = true
    api('/service-appointments').then((data) => { if (active) setBookings({ revision, data }) })
      .catch((err) => {
        if (!active) return
        if (err.status === 401) logout()
        setBookings({ revision, error: err.message })
      })
    return () => { active = false }
  }, [revision, logout])

  const ready = calendar?.key === key
  const listReady = bookings?.revision === revision
  const slots = ready && calendar.data ? calendar.data.slots : []
  const selectable = (s) => (s.available || (editing && new Date(editing.appointmentDate).getTime() === new Date(s.startsAt).getTime())) && new Date(s.startsAt).getTime() > now
  const daySlots = slots.filter((s) => dayOf(s.startsAt) === day)
  const picked = daySlots.find((s) => new Date(s.startsAt).getTime() === new Date(slot).getTime() && selectable(s))
  const padding = (new Date(`${month}-01T12:00:00Z`).getUTCDay() + 6) % 7
  const refresh = () => setRevision((n) => n + 1)
  function reset() { setEditing(null); setForm(empty); setSlot(''); setError('') }
  function edit(item) {
    const date = dayOf(item.appointmentDate)
    setEditing(item); setMonth(date.slice(0, 7)); setDay(date); setSlot(item.appointmentDate)
    setForm({ bikeType: item.bikeType, serviceType: item.serviceType, problemDescription: item.problemDescription ?? '' })
    setMessage(''); setError('')
    document.getElementById('service-booking').scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
  async function submit(event) {
    event.preventDefault()
    if (!picked || busy) return
    setBusy(true); setError(''); setMessage('')
    try {
      await api(`/service-appointments${editing ? `/${editing.id}` : ''}`, { method: editing ? 'PUT' : 'POST',
        body: { ...form, bikeType: form.bikeType.trim(), serviceType: form.serviceType.trim(), appointmentDate: picked.startsAt } })
      setMessage(editing ? 'A foglalás módosítva. Új visszaigazolásra vár.' : 'Sikeres időpontfoglalás! A foglalásod visszaigazolásra vár.')
      reset(); refresh()
    } catch (err) {
      if (err.status === 401) logout()
      setError(err.message)
      if (err.status === 409) { setSlot(''); refresh() }
    } finally { setBusy(false) }
  }
  async function cancel(id) {
    setBusy(true); setError(''); setMessage('')
    try {
      await api(`/service-appointments/${id}`, { method: 'DELETE' })
      if (editing?.id === id) reset()
      setMessage('Az időpontot lemondtad.'); setCancelId(null); refresh()
    } catch (err) {
      if (err.status === 401) logout()
      setError(err.message); refresh()
    } finally { setBusy(false) }
  }

  return <main className="service-page page-width">
    <div className="profile-heading"><div><span className="section-kicker">GONDOSKODJ A BRINGÁDRÓL</span><h1>Szervizidőpont</h1><p>Válassz egy szabad időpontot, a többit bízd ránk.</p></div><Link className="button-secondary" to="/">← Főoldal</Link></div>
    {message && <p className="alert success" role="status">{message}</p>}
    {error && <p className="alert error" role="alert">{error}</p>}
    <section className="service-layout" id="service-booking" aria-label="Időpontfoglalás">
      <div className="service-panel">
        <span className="section-kicker">01 / VÁLASSZ IDŐPONTOT</span>
        <div className="calendar-heading"><h2 id="calendar-month">{format(`${month}-01T12:00:00Z`, { year: 'numeric', month: 'long' })}</h2><div className="calendar-controls">
          <button type="button" aria-label="Előző hónap" disabled={busy || month <= today.slice(0, 7)} onClick={() => { setMonth(shiftMonth(month, -1)); setSlot('') }}>←</button>
          <button type="button" aria-label="Következő hónap" disabled={busy || month >= horizon.slice(0, 7)} onClick={() => { setMonth(shiftMonth(month, 1)); setSlot('') }}>→</button>
        </div></div>
        <div className="service-calendar" role="group" aria-labelledby="calendar-month" aria-busy={!ready}>
          {['H', 'K', 'Sze', 'Cs', 'P', 'Szo', 'V'].map((name, i) => <span className="weekday" key={i}>{name}</span>)}
          {Array.from({ length: padding }, (_, i) => <span key={`pad-${i}`} />)}
          {Array.from({ length: Number(monthEnd.slice(-2)) }, (_, i) => {
            const date = `${month}-${String(i + 1).padStart(2, '0')}`
            const available = slots.some((s) => dayOf(s.startsAt) === date && selectable(s))
            return <button type="button" key={date} aria-pressed={day === date} aria-current={date === today ? 'date' : undefined}
              aria-label={`${format(`${date}T12:00:00Z`, { month: 'long', day: 'numeric' })}${available ? ', szabad időpont' : ', nincs szabad időpont'}`}
              disabled={busy || !ready || !available} className={`calendar-day${day === date ? ' selected' : ''}`}
              onClick={() => { setDay(date); setSlot('') }}><span>{i + 1}</span>{available && <i aria-hidden="true" />}</button>
          })}
        </div>
        {!ready && <p className="service-muted" role="status">Időpontok betöltése…</p>}
        {ready && calendar.error && <div className="alert error" role="alert">{calendar.error} <button type="button" className="service-link" onClick={refresh}>Újrapróbálás</button></div>}
        <p className="calendar-legend"><span aria-hidden="true" /> Szabad időponttal rendelkező nap</p>
        <div className="service-times" aria-label="Választható időpontok">
          <h3>{day.slice(0, 7) === month ? format(`${day}T12:00:00Z`, { month: 'long', day: 'numeric', weekday: 'long' }) : 'Válassz egy napot a naptárban'}</h3>
          {ready && !calendar.error && day.slice(0, 7) === month && <>
            <div className="time-grid">{daySlots.map((s) => <button type="button" key={s.startsAt} disabled={busy || !selectable(s)} aria-pressed={picked?.startsAt === s.startsAt}
              className={picked?.startsAt === s.startsAt ? 'selected' : ''} onClick={() => setSlot(s.startsAt)}>{format(s.startsAt, { hour: '2-digit', minute: '2-digit' })}<small>{selectable(s) ? 'Szabad' : 'Foglalt'}</small></button>)}</div>
            {daySlots.length === 0 && <p className="service-muted">Ezen a napon nincs foglalható időpont. Válassz másik napot.</p>}
          </>}
        </div>
        <p className="service-muted">Budapesti helyi idő szerint. Legfeljebb 90 nappal előre foglalhatsz.</p>
      </div>
      <form className="service-panel service-form" onSubmit={submit}>
        <span className="section-kicker">02 / A KERÉKPÁROD ADATAI</span><h2>{editing ? 'Foglalás módosítása' : 'Miben segíthetünk?'}</h2>
        {editing && <p className="service-muted">#{editing.id} · A módosítás után új visszaigazolást kérünk.</p>}
        <fieldset disabled={busy}>
          <label>Kerékpár típusa<input required maxLength={100} name="bikeType" placeholder="Pl. MTB, országúti, városi" value={form.bikeType} onChange={(e) => setForm({ ...form, bikeType: e.target.value })} /></label>
          <label>Kért szerviz<input required maxLength={100} name="serviceType" placeholder="Pl. átvizsgálás, fékbeállítás" value={form.serviceType} onChange={(e) => setForm({ ...form, serviceType: e.target.value })} /></label>
          <label>Probléma leírása (nem kötelező)<textarea name="problemDescription" rows={4} maxLength={1000} placeholder="Írd le, mit tapasztaltál a kerékpáron…" value={form.problemDescription} onChange={(e) => setForm({ ...form, problemDescription: e.target.value })} /></label>
          <small className="service-muted">{form.problemDescription.length}/1000 karakter</small>
        </fieldset>
        <div className="booking-summary"><span>Kiválasztott időpont</span><strong>{picked ? format(picked.startsAt, { dateStyle: 'long', timeStyle: 'short' }) : 'Válassz napot és időpontot a naptárban.'}</strong>{picked && <small>{calendar.data.slotMinutes} perces időpont</small>}</div>
        <button className="button-primary" type="submit" disabled={busy || !picked || !form.bikeType.trim() || !form.serviceType.trim()}>{busy ? 'Mentés…' : editing ? 'Módosítás mentése' : 'Időpont lefoglalása'} <span aria-hidden="true">→</span></button>
        {editing && <button className="button-secondary" type="button" disabled={busy} onClick={reset}>Módosítás elvetése</button>}
      </form>
    </section>
    <section className="service-bookings" aria-labelledby="my-bookings">
      <div className="service-list-heading"><div><span className="section-kicker">MINDEN EGY HELYEN</span><h2 id="my-bookings">Saját foglalásaim</h2></div><button type="button" className="button-secondary" disabled={busy || !listReady} onClick={refresh}>Frissítés ↻</button></div>
      {!listReady && <p role="status">Foglalások betöltése…</p>}
      {listReady && bookings.error && <p className="alert error" role="alert">{bookings.error} <button type="button" className="service-link" onClick={refresh}>Újrapróbálás</button></p>}
      {listReady && bookings.data?.length === 0 && <div className="service-empty"><span aria-hidden="true">✳</span><h3>Még nincs szervizfoglalásod</h3><p>Válassz egy szabad időpontot a fenti naptárban.</p></div>}
      {listReady && bookings.data?.map((item) => {
        const mutable = ['Pending', 'Confirmed'].includes(item.status) && new Date(item.appointmentDate).getTime() > now
        return <article className="appointment-card" key={item.id}>
          <div className="appointment-details"><span className={`service-status status-${item.status}`}>{labels[item.status] ?? item.status}</span><h3>{format(item.appointmentDate, { dateStyle: 'long', timeStyle: 'short' })}</h3><p><strong>{item.bikeType}</strong> · {item.serviceType}</p>{item.problemDescription && <p className="appointment-description">{item.problemDescription}</p>}</div>
          {mutable && <div className="appointment-actions">{cancelId === item.id ? <><p>Biztosan lemondod ezt az időpontot?</p><button type="button" className="service-danger" disabled={busy} onClick={() => cancel(item.id)}>Igen, lemondom</button><button type="button" className="button-secondary" disabled={busy} onClick={() => setCancelId(null)}>Mégsem</button></> : <><button type="button" className="button-secondary" disabled={busy} onClick={() => edit(item)}>Módosítás</button><button type="button" className="service-danger" disabled={busy} onClick={() => setCancelId(item.id)}>Lemondás</button></>}</div>}
        </article>
      })}
    </section>
  </main>
}
