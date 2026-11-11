export const bikeTypes = [
  { value: 0, label: 'MTB' },
  { value: 1, label: 'Trekking' },
  { value: 2, label: 'City' },
  { value: 3, label: 'E-Bike' },
  { value: 4, label: 'Gyerek' }
]

export const bikeTypeLabel = (value) => bikeTypes.find((t) => t.value === value)?.label ?? value

export const money = (value) => `${Number(value).toLocaleString('hu-HU')} Ft`

export const formatDate = (value, options = { dateStyle: 'medium' }) =>
  new Intl.DateTimeFormat('hu-HU', options).format(new Date(value))

// yyyy-mm-dd helyi idő szerint (date inputhoz)
export function isoDay(date = new Date()) {
  const pad = (n) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function addDays(day, count) {
  const date = new Date(`${day}T12:00:00`)
  date.setDate(date.getDate() + count)
  return isoDay(date)
}
