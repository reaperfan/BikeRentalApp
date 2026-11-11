const DAY_MS = 24 * 60 * 60 * 1000

export function toISODate(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function parseISODate(value) {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number)
  return new Date(year, month - 1, day)
}

export function toDay(value) {
  return String(value).slice(0, 10)
}

export function addDays(isoDate, days) {
  const date = parseISODate(isoDate)
  return toISODate(new Date(date.getFullYear(), date.getMonth(), date.getDate() + days))
}

export function daysBetween(start, end) {
  return Math.round((parseISODate(end) - parseISODate(start)) / DAY_MS)
}

export function getBookedDays(rentals, bikeId) {
  const booked = new Set()
  rentals
    .filter(rental => rental.bikeId === bikeId)
    .forEach(rental => {
      const end = toDay(rental.endDate)
      for (let day = toDay(rental.startDate); day < end; day = addDays(day, 1)) {
        booked.add(day)
      }
    })
  return booked
}

export function isRangeAvailable(rentals, bikeId, start, end) {
  return !rentals.some(
    rental =>
      rental.bikeId === bikeId &&
      toDay(rental.startDate) < end &&
      toDay(rental.endDate) > start
  )
}

export function isRangeFree(bookedDays, start, end) {
  for (let day = start; day <= end; day = addDays(day, 1)) {
    if (bookedDays.has(day)) return false
  }
  return true
}
