import { describe, expect, it } from 'vitest'
import {
  addDays,
  daysBetween,
  getBookedDays,
  isRangeAvailable,
  isRangeFree,
  toISODate
} from './rentalDates'

const rentals = [
  { bikeId: 1, startDate: '2030-05-10', endDate: '2030-05-13' },
  { bikeId: 2, startDate: '2030-05-10', endDate: '2030-05-12' }
]

describe('toISODate / addDays', () => {
  it('formats a local date as YYYY-MM-DD', () => {
    expect(toISODate(new Date(2030, 0, 5))).toBe('2030-01-05')
  })

  it('adds days across month boundaries', () => {
    expect(addDays('2030-01-31', 1)).toBe('2030-02-01')
  })
})

describe('daysBetween', () => {
  it('counts the days between two dates', () => {
    expect(daysBetween('2030-05-10', '2030-05-13')).toBe(3)
  })
})

describe('getBookedDays', () => {
  it('marks every day of the rental except the exclusive end date', () => {
    const booked = getBookedDays(rentals, 1)
    expect([...booked].sort()).toEqual(['2030-05-10', '2030-05-11', '2030-05-12'])
  })

  it('ignores rentals of other bikes', () => {
    expect(getBookedDays(rentals, 3).size).toBe(0)
  })
})

describe('isRangeAvailable', () => {
  it('is false when the range overlaps a rental', () => {
    expect(isRangeAvailable(rentals, 1, '2030-05-11', '2030-05-14')).toBe(false)
  })

  it('is true when the range starts on the rental end date (exclusive)', () => {
    expect(isRangeAvailable(rentals, 1, '2030-05-13', '2030-05-15')).toBe(true)
  })

  it('is true when the range ends on the rental start date (exclusive)', () => {
    expect(isRangeAvailable(rentals, 1, '2030-05-07', '2030-05-10')).toBe(true)
  })

  it('is true for another bike', () => {
    expect(isRangeAvailable(rentals, 3, '2030-05-10', '2030-05-13')).toBe(true)
  })
})

describe('isRangeFree', () => {
  it('checks every day of the range, including the end date', () => {
    const booked = new Set(['2030-05-12'])
    expect(isRangeFree(booked, '2030-05-10', '2030-05-11')).toBe(true)
    expect(isRangeFree(booked, '2030-05-10', '2030-05-12')).toBe(false)
  })
})
