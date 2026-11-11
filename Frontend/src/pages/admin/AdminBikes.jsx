import { useCallback, useState } from 'react'
import { createBike, deleteBike, updateBike, uploadBikeImage } from '../../api/adminService'
import { getBikes } from '../../api/bikeService'
import useAdminResource from '../../hooks/useAdminResource'
import Failure from './Failure'
import { bikeTypeLabel, bikeTypes, money } from './format'

const empty = { name: '', type: 0, dailyPrice: '', imageUrl: '', isAvailable: true }
const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_IMAGE_BYTES = 5 * 1024 * 1024

export default function AdminBikes() {
  const load = useCallback(() => getBikes(), [])
  const [bikes, reload] = useAdminResource(load)
  const [form, setForm] = useState(empty)
  const [editingId, setEditingId] = useState(null)
  const [deleteId, setDeleteId] = useState(null)
  const [busy, setBusy] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const images = [...new Set((bikes.data ?? []).map((b) => b.imageUrl))].sort()
  const price = Number(form.dailyPrice)
  const valid = form.name.trim() && price > 0 && form.imageUrl.trim()

  function reset() {
    setForm(empty)
    setEditingId(null)
  }

  function edit(bike) {
    setForm({ name: bike.name, type: bike.type, dailyPrice: String(bike.dailyPrice), imageUrl: bike.imageUrl, isAvailable: bike.isAvailable })
    setEditingId(bike.id)
    setMessage('')
    setError('')
    document.getElementById('admin-bike-form').scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  async function run(action, success) {
    setBusy(true)
    setError('')
    setMessage('')
    try {
      await action()
      setMessage(success)
      reload()
      return true
    } catch (err) {
      setError(err.message)
      return false
    } finally {
      setBusy(false)
    }
  }

  async function pickImage(event) {
    const file = event.target.files[0]
    event.target.value = ''
    if (!file) return
    setError('')
    setMessage('')
    // Gyors ellenőrzés a böngészőben; a backend a tartalmat is megvizsgálja
    if (!IMAGE_TYPES.includes(file.type)) return setError('Csak JPG, PNG vagy WEBP kép tölthető fel.')
    if (file.size > MAX_IMAGE_BYTES) return setError('A kép legfeljebb 5 MB lehet.')

    setUploading(true)
    try {
      const { url } = await uploadBikeImage(file)
      setForm((f) => ({ ...f, imageUrl: url }))
      setMessage(`${file.name} feltöltve.`)
    } catch (err) {
      setError(err.message)
    } finally {
      setUploading(false)
    }
  }

  async function submit(event) {
    event.preventDefault()
    if (!valid || busy) return
    const body = { name: form.name.trim(), type: Number(form.type), dailyPrice: price, imageUrl: form.imageUrl.trim() }
    const ok = editingId
      ? await run(() => updateBike(editingId, { ...body, isAvailable: form.isAvailable }), 'A bicikli módosítva.')
      : await run(() => createBike(body), 'Új bicikli felvéve.')
    if (ok) reset()
  }

  const toggle = (bike) => run(
    () => updateBike(bike.id, { ...bike, isAvailable: !bike.isAvailable }),
    bike.isAvailable ? `${bike.name} nem elérhető.` : `${bike.name} újra elérhető.`)

  async function remove(id) {
    if (await run(() => deleteBike(id), 'A bicikli törölve.') && editingId === id) reset()
    setDeleteId(null)
  }

  return (
    <>
      {message && <p className="alert success" role="status">{message}</p>}
      {error && <p className="alert error" role="alert">{error}</p>}

      <form className="admin-form" id="admin-bike-form" onSubmit={submit}>
        <h2>{editingId ? `Bicikli módosítása (#${editingId})` : 'Új bicikli felvétele'}</h2>
        <fieldset disabled={busy || uploading}>
          <label>Név<input required maxLength={100} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="Pl. Trek Marlin 7" /></label>
          <label>Típus
            <select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}>
              {bikeTypes.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
            </select>
          </label>
          <label>Napidíj (Ft)<input required type="number" min="1" step="1" inputMode="numeric" value={form.dailyPrice} onChange={(e) => setForm({ ...form, dailyPrice: e.target.value })} placeholder="Pl. 6500" /></label>
          <div className="admin-form-wide admin-image-field">
            <span>Kép</span>
            <div className="admin-image-picker">
              <label className="button-secondary admin-file-button">
                {uploading ? 'Feltöltés…' : 'Kép tallózása…'}
                <input type="file" accept={IMAGE_TYPES.join(',')} disabled={uploading} onChange={pickImage} />
              </label>
              <small>JPG, PNG vagy WEBP, legfeljebb 5 MB.</small>
            </div>
            <label>vagy meglévő kép útvonala
              <input required list="admin-bike-images" value={form.imageUrl} onChange={(e) => setForm({ ...form, imageUrl: e.target.value })} />
            </label>
            <datalist id="admin-bike-images">{images.map((src) => <option key={src} value={src} />)}</datalist>
          </div>
          {editingId && <label className="admin-check"><input type="checkbox" checked={form.isAvailable} onChange={(e) => setForm({ ...form, isAvailable: e.target.checked })} /> Elérhető kölcsönzésre</label>}
        </fieldset>
        {form.imageUrl.trim() && <img className="admin-bike-preview" src={form.imageUrl} alt="Kép előnézet" onError={(e) => { e.currentTarget.style.visibility = 'hidden' }} onLoad={(e) => { e.currentTarget.style.visibility = 'visible' }} />}
        <div className="admin-form-actions">
          <button className="button-primary" type="submit" disabled={busy || uploading || !valid}>{busy ? 'Mentés…' : editingId ? 'Módosítás mentése' : 'Bicikli felvétele'} <span aria-hidden="true">→</span></button>
          {editingId && <button className="button-secondary" type="button" disabled={busy} onClick={reset}>Mégsem</button>}
        </div>
      </form>

      <div className="admin-panel-heading">
        <h2>Biciklik {bikes.data && <small>({bikes.data.length})</small>}</h2>
        <button type="button" className="button-secondary" disabled={busy} onClick={reload}>Frissítés ↻</button>
      </div>
      {bikes.loading && <p role="status">Biciklik betöltése…</p>}
      {bikes.error && <Failure message={bikes.error} retry={reload} />}
      {bikes.data && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead><tr><th>Kép</th><th>Név</th><th>Típus</th><th>Napidíj</th><th>Állapot</th><th><span className="visually-hidden">Műveletek</span></th></tr></thead>
            <tbody>
              {bikes.data.map((bike) => (
                <tr key={bike.id} className={editingId === bike.id ? 'selected' : ''}>
                  <td><img className="admin-thumb" src={bike.imageUrl} alt="" /></td>
                  <td><strong>{bike.name}</strong><small>#{bike.id}</small></td>
                  <td>{bikeTypeLabel(bike.type)}</td>
                  <td>{money(bike.dailyPrice)}</td>
                  <td><span className={`admin-badge ${bike.isAvailable ? 'ok' : 'off'}`}>{bike.isAvailable ? 'Elérhető' : 'Nem elérhető'}</span></td>
                  <td className="admin-actions">
                    {deleteId === bike.id ? (
                      <>
                        <span>Biztosan törlöd?</span>
                        <button type="button" className="service-danger" disabled={busy} onClick={() => remove(bike.id)}>Igen, törlöm</button>
                        <button type="button" className="button-secondary" disabled={busy} onClick={() => setDeleteId(null)}>Mégsem</button>
                      </>
                    ) : (
                      <>
                        <button type="button" className="button-secondary" disabled={busy} onClick={() => edit(bike)}>Szerkesztés</button>
                        <button type="button" className="button-secondary" disabled={busy} onClick={() => toggle(bike)}>{bike.isAvailable ? 'Letiltás' : 'Engedélyezés'}</button>
                        <button type="button" className="service-danger" disabled={busy} onClick={() => setDeleteId(bike.id)}>Törlés</button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
