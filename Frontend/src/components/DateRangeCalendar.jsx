import { useState } from 'react'
import { daysBetween, isRangeFree, parseISODate, toISODate } from '../utils/rentalDates'
import '../styles/DateRangeCalendar.css'

const WEEKDAYS = ['H', 'K', 'Sze', 'Cs', 'P', 'Szo', 'V']

export default function DateRangeCalendar({ start, end, bookedDays, minDate, onChange }) {
  const [month, setMonth] = useState(() => {
    const base = parseISODate(start || minDate)
    return new Date(base.getFullYear(), base.getMonth(), 1)
  })

  const year = month.getFullYear()
  const monthIndex = month.getMonth()
  const leadingEmpty = (month.getDay() + 6) % 7
  const daysInMonth = new Date(year, monthIndex + 1, 0).getDate()
  const cells = [
    ...Array(leadingEmpty).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => toISODate(new Date(year, monthIndex, i + 1)))
  ]

  function shiftMonth(delta) {
    setMonth(new Date(year, monthIndex + delta, 1))
  }

  function handleDayClick(day) {
    if (day < minDate || bookedDays.has(day)) return
    if (!start || end || day <= start) {
      onChange({ start: day, end: null })
      return
    }
    onChange(isRangeFree(bookedDays, start, day) ? { start, end: day } : { start: day, end: null })
  }

  function renderDay(day) {
    const isBooked = bookedDays.has(day)
    const isDisabled = day < minDate || isBooked
    const isEdge = day === start || day === end
    const inRange = !!(start && end && day >= start && day <= end)
    const className = ['calendar-day', isBooked && 'booked', inRange && 'in-range', isEdge && 'edge']
      .filter(Boolean)
      .join(' ')

    return (
      <button
        key={day}
        type="button"
        className={className}
        disabled={isDisabled}
        onClick={() => handleDayClick(day)}
      >
        {Number(day.slice(8))}
      </button>
    )
  }

  const hint = !start
    ? 'Kattints a kezdő napra'
    : !end
      ? 'Kattints a befejező napra'
      : `${daysBetween(start, end)} nap kiválasztva`

  return (
    <div className="date-range-calendar">
      <div className="calendar-header">
        <button type="button" onClick={() => shiftMonth(-1)} aria-label="Előző hónap">‹</button>
        <span className="calendar-title">
          {month.toLocaleDateString('hu-HU', { year: 'numeric', month: 'long' })}
        </span>
        <button type="button" onClick={() => shiftMonth(1)} aria-label="Következő hónap">›</button>
      </div>

      <div className="calendar-weekdays">
        {WEEKDAYS.map(weekday => <span key={weekday}>{weekday}</span>)}
      </div>

      <div className="calendar-grid">
        {cells.map((day, i) =>
          day === null ? <span key={`empty-${i}`} /> : renderDay(day)
        )}
      </div>

      <div className="calendar-legend">
        <span><i className="legend-dot legend-selected" />Kiválasztott</span>
        <span><i className="legend-dot legend-booked" />Foglalt</span>
      </div>

      <p className="calendar-hint">{hint}</p>
    </div>
  )
}
