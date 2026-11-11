import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import DateRangeCalendar from './DateRangeCalendar'

const minDate = '2030-05-01'

describe('DateRangeCalendar', () => {
  it('renders the month of the start date with a button per day', () => {
    render(
      <DateRangeCalendar start="2030-05-10" end={null} bookedDays={new Set()} minDate={minDate} onChange={vi.fn()} />
    )
    expect(screen.getByText(/2030/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '31' })).toBeInTheDocument()
  })

  it('disables days before the minimum date', () => {
    render(
      <DateRangeCalendar start={null} end={null} bookedDays={new Set()} minDate="2030-05-10" onChange={vi.fn()} />
    )
    expect(screen.getByRole('button', { name: '9' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '10' })).toBeEnabled()
  })

  it('disables booked days', () => {
    render(
      <DateRangeCalendar
        start={null}
        end={null}
        bookedDays={new Set(['2030-05-11'])}
        minDate={minDate}
        onChange={vi.fn()}
      />
    )
    expect(screen.getByRole('button', { name: '11' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '11' })).toHaveClass('booked')
  })

  it('selects a start day first', () => {
    const onChange = vi.fn()
    render(
      <DateRangeCalendar start={null} end={null} bookedDays={new Set()} minDate={minDate} onChange={onChange} />
    )
    fireEvent.click(screen.getByRole('button', { name: '10' }))
    expect(onChange).toHaveBeenCalledWith({ start: '2030-05-10', end: null })
  })

  it('selects an end day when the range is free', () => {
    const onChange = vi.fn()
    render(
      <DateRangeCalendar start="2030-05-10" end={null} bookedDays={new Set()} minDate={minDate} onChange={onChange} />
    )
    fireEvent.click(screen.getByRole('button', { name: '14' }))
    expect(onChange).toHaveBeenCalledWith({ start: '2030-05-10', end: '2030-05-14' })
  })

  it('restarts the selection when the range would cross a booked day', () => {
    const onChange = vi.fn()
    render(
      <DateRangeCalendar
        start="2030-05-10"
        end={null}
        bookedDays={new Set(['2030-05-11'])}
        minDate={minDate}
        onChange={onChange}
      />
    )
    fireEvent.click(screen.getByRole('button', { name: '14' }))
    expect(onChange).toHaveBeenCalledWith({ start: '2030-05-14', end: null })
  })

  it('moves to the next month', () => {
    render(
      <DateRangeCalendar start={null} end={null} bookedDays={new Set()} minDate={minDate} onChange={vi.fn()} />
    )
    fireEvent.click(screen.getByRole('button', { name: 'Következő hónap' }))
    expect(screen.getByRole('button', { name: '30' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '31' })).not.toBeInTheDocument()
  })
})
