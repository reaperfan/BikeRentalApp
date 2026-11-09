import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { useReviewRequest } from '../hooks/useReviewRequest'
import '../styles/Reviews.css'

const date = (value) => new Intl.DateTimeFormat('hu-HU', { timeZone: 'Europe/Budapest', dateStyle: 'long' }).format(new Date(value))
const number = (value) => new Intl.NumberFormat('hu-HU', { maximumFractionDigits: 1, minimumFractionDigits: 1 }).format(value)
function Stars({ rating }) {
  return <span className="review-stars" role="img" aria-label={`${rating} az 5 csillagból`}>
    {Array.from({ length: 5 }, (_, i) => <span aria-hidden="true" className={i < rating ? 'filled' : ''} key={i}>★</span>)}
  </span>
}
function Failure({ message, retry }) {
  return <div className="alert error" role="alert">{message} <button type="button" className="review-text-button" onClick={retry}>Újrapróbálás</button></div>
}
function DeleteReview({ id, admin = false, onDeleted, disabled = false, onBusyChange }) {
  const { logout } = useAuth()
  const [confirming, setConfirming] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function remove() {
    if (busy) return
    setBusy(true); setError(''); onBusyChange?.(true)
    try {
      await api(`/reviews/${admin ? 'admin/' : ''}${id}`, { method: 'DELETE' })
      onDeleted('Az értékelést töröltük.')
    } catch (err) {
      if (err.status === 401) logout()
      setError(err.message)
    } finally { setBusy(false); onBusyChange?.(false) }
  }
  return <div className="review-delete">
    {error && <p className="alert error" role="alert">{error}</p>}
    {confirming ? <div className="review-delete-confirm"><p>Biztosan törlöd ezt az értékelést?</p><button className="review-danger" type="button" disabled={busy || disabled} onClick={remove}>{busy ? 'Törlés…' : 'Igen, törlöm'}</button><button className="button-secondary" type="button" disabled={busy || disabled} onClick={() => setConfirming(false)}>Mégsem</button></div>
      : <button className="review-danger" type="button" disabled={disabled} onClick={() => setConfirming(true)}>{admin ? 'Moderálás: törlés' : 'Törlés'}</button>}
  </div>
}

function MyReviews({ revision, onChanged }) {
  const { logout } = useAuth()
  const mine = useReviewRequest('/reviews/mine', revision, true)
  const [editing, setEditing] = useState(null)
  const [rating, setRating] = useState(0)
  const [comment, setComment] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const retry = () => onChanged('')
  function reset() { setEditing(null); setRating(0); setComment(''); setError('') }
  function edit(item) {
    setEditing(item); setRating(item.rating); setComment(item.comment ?? ''); setError('')
    document.getElementById('review-form').scrollIntoView({ behavior: 'smooth', block: 'start' })
    document.getElementById('review-form-heading').focus({ preventScroll: true })
  }
  async function submit(event) {
    event.preventDefault()
    if (busy || rating < 1) return
    setBusy(true); setError('')
    try {
      await api(`/reviews${editing ? `/${editing.id}` : ''}`, { method: editing ? 'PUT' : 'POST',
        body: { rating, comment: comment.trim() || null } })
      const message = editing ? 'Az értékelésed módosítottuk.' : 'Köszönjük az értékelésed!'
      reset(); onChanged(message)
    } catch (err) {
      if (err.status === 401) logout()
      setError(err.message)
      if (err.status === 404) onChanged('')
    } finally { setBusy(false) }
  }
  return <section className="my-reviews" aria-label="Saját értékelések kezelése">
    <form className="review-panel review-form" id="review-form" onSubmit={submit}>
      <span className="section-kicker">A TE TAPASZTALATOD IS SZÁMÍT</span>
      <h2 id="review-form-heading" tabIndex={-1}>{editing ? 'Értékelés módosítása' : 'Mondd el a véleményed!'}</h2>
      {error && <p className="alert error" role="alert">{error}</p>}
      <fieldset className="rating-field" disabled={busy}><legend>Hány csillagot adnál?</legend><div className="rating-options">{[1, 2, 3, 4, 5].map((value) => <label key={value} className={value <= rating ? 'filled' : ''}><input type="radio" name="rating" value={value} checked={rating === value} onChange={() => setRating(value)} required aria-label={`${value} csillag`} /><span aria-hidden="true">★</span></label>)}</div><span className="review-muted" aria-live="polite">{rating ? `${rating} / 5 csillag` : 'Válassz 1–5 csillagot'}</span></fieldset>
      <label>Vélemény (nem kötelező)<textarea rows={4} maxLength={2000} value={comment} disabled={busy} placeholder="Mi tetszett, és min javíthatnánk?" onChange={(e) => setComment(e.target.value)} /></label>
      <span className="review-counter">{comment.length}/2000 karakter</span>
      <button className="button-primary" type="submit" disabled={busy || rating < 1}>{busy ? 'Mentés…' : editing ? 'Módosítás mentése' : 'Értékelés elküldése'} <span aria-hidden="true">→</span></button>
      {editing && <button className="button-secondary" type="button" disabled={busy} onClick={reset}>Módosítás elvetése</button>}
    </form>
    <div className="review-panel own-reviews"><span className="section-kicker">AMIT TE ÍRTÁL</span><h2>Saját értékeléseim</h2>
      {mine.loading && <p role="status">Értékeléseid betöltése…</p>}
      {mine.error && <Failure message={mine.error} retry={retry} />}
      {mine.data?.length === 0 && <p className="review-muted">Még nem írtál értékelést. Oszd meg velünk a véleményed!</p>}
      {mine.data?.map((item) => <article className="own-review" key={item.id}><div className="review-card-top"><Stars rating={item.rating} /><time dateTime={item.createdAt}>{date(item.createdAt)}</time></div>{item.comment && <p className="review-comment">{item.comment}</p>}<div className="review-actions"><button type="button" className="button-secondary" disabled={busy} onClick={() => edit(item)}>Módosítás</button><DeleteReview id={item.id} disabled={busy} onBusyChange={setBusy} onDeleted={(message) => { if (editing?.id === item.id) reset(); onChanged(message) }} /></div></article>)}
    </div>
  </section>
}

export default function Reviews() {
  const { user } = useAuth()
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState('')
  const [revision, setRevision] = useState(0)
  const [message, setMessage] = useState('')
  const summary = useReviewRequest('/reviews/summary', revision)
  const list = useReviewRequest(`/reviews?page=${page}&pageSize=6${filter ? `&rating=${filter}` : ''}`, revision)
  function refresh(notice = '') { setRevision((n) => n + 1); setPage(1); setMessage(notice) }
  const totals = summary.data
  const totalPages = list.data ? Math.max(1, Math.ceil(list.data.totalCount / list.data.pageSize)) : 1
  return <main className="reviews-page page-width">
    <div className="review-hero"><div><h1>Értékelések</h1><p>Válassz csillagot, és írd meg a véleményed!</p></div></div>
    {message && <p className="alert success review-notice" role="status">{message}</p>}
    <section className="review-overview" aria-label="Értékelések összesítése">
      <div className="review-score"><span className="section-kicker">ÍGY LÁTTOK MINKET</span>{summary.loading ? <p role="status">Összesítés betöltése…</p> : summary.error ? <Failure message={summary.error} retry={() => refresh()} /> : <><div className="score-value">{totals.averageRating == null ? '–' : number(totals.averageRating)}<span>/ 5</span></div><Stars rating={Math.round(totals.averageRating ?? 0)} /><p>{totals.totalCount === 0 ? 'Még nincs értékelés' : `${totals.totalCount} értékelés alapján`}</p></>}</div>
    </section>
    {user ? <MyReviews key={user.id} revision={revision} onChanged={refresh} /> : <div className="review-login"><div><h2>Oszd meg a tapasztalatod!</h2><p>Értékelés írásához jelentkezz be a fiókodba.</p></div><Link className="button-primary" to="/login" state={{ from: { pathname: '/ertekelesek' } }}>Bejelentkezés →</Link></div>}
    <section className="public-reviews" aria-labelledby="public-review-heading"><div className="review-list-heading"><div><span className="section-kicker">A KÖZÖSSÉG VISSZAJELZÉSEI</span><h2 id="public-review-heading">Értékelések</h2></div><label>Szűrés pontszám szerint<select value={filter} onChange={(e) => { setFilter(e.target.value); setPage(1) }}><option value="">Minden értékelés</option>{[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value} csillag</option>)}</select></label></div>
      {list.loading && <p className="review-loading" role="status">Értékelések betöltése…</p>}
      {list.error && <Failure message={list.error} retry={() => refresh()} />}
      {list.data?.items.length === 0 && <div className="review-empty"><h3>{filter ? 'Ilyen pontszámú értékelés még nincs' : 'Még nincs megjeleníthető értékelés'}</h3><p>{filter ? 'Válassz másik szűrőt, vagy nézd meg az összes véleményt.' : 'Legyél az első, aki megosztja a véleményét!'}</p></div>}
      <div className="review-grid">{list.data?.items.map((item) => <article className="public-review review-panel" key={item.id}><div className="review-card-top"><Stars rating={item.rating} /><time dateTime={item.createdAt}>{date(item.createdAt)}</time></div><p className={`review-comment${item.comment ? '' : ' review-muted'}`}>{item.comment || 'Szöveges vélemény nélkül.'}</p><div className="review-author"><span aria-hidden="true">{item.authorName?.[0]?.toUpperCase() || 'B'}</span><strong>{item.authorName}</strong></div>{user?.role === 'Admin' && <DeleteReview id={item.id} admin onDeleted={refresh} />}</article>)}</div>
      <nav className="review-pagination" aria-label="Értékelések lapozása"><button className="button-secondary" type="button" disabled={page <= 1 || list.loading} onClick={() => setPage((n) => n - 1)}>← Előző</button><span aria-live="polite">{page}. oldal{list.data ? ` / ${totalPages} · ${list.data.totalCount} értékelés` : ''}</span><button className="button-secondary" type="button" disabled={!list.data || page >= totalPages || list.loading} onClick={() => setPage((n) => n + 1)}>Következő →</button></nav>
    </section>
  </main>
}
