import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import BikeCatalog from './BikeCatalog'
import { getBikes, getRentals } from '../api/bikeService'
import { useAuth } from '../auth/AuthContext'

vi.mock('../api/bikeService', () => ({
  getBikes: vi.fn(),
  getRentals: vi.fn()
}))

vi.mock('../auth/AuthContext', () => ({
  useAuth: vi.fn()
}))

vi.mock('../components/DateRangeCalendar', () => ({
  default: ({ onChange }) => (
    <button type="button" onClick={() => onChange({ start: '2030-05-10', end: '2030-05-12' })}>
      mock-select-range
    </button>
  )
}))

const bikes = [
  { id: 1, name: 'City 1', type: 2, dailyPrice: 100, isAvailable: true, imageUrl: '' },
  { id: 2, name: 'MTB 2', type: 0, dailyPrice: 200, isAvailable: true, imageUrl: '' }
]

const rentals = [{ bikeId: 1, startDate: '2030-05-11', endDate: '2030-05-14' }]

function renderCatalog() {
  return render(
    <MemoryRouter>
      <BikeCatalog />
    </MemoryRouter>
  )
}

describe('BikeCatalog', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
    getBikes.mockResolvedValue(bikes)
    getRentals.mockResolvedValue(rentals)
    useAuth.mockReturnValue({ user: { username: 'anna', email: 'anna@example.com' } })
  })

  it('shows all bikes before a date range is selected', async () => {
    renderCatalog()
    expect(await screen.findByText('City 1')).toBeInTheDocument()
    expect(screen.getByText('MTB 2')).toBeInTheDocument()
  })

  it('hides bikes that are booked in the selected range', async () => {
    renderCatalog()
    await screen.findByText('City 1')
    fireEvent.click(screen.getByRole('button', { name: 'mock-select-range' }))
    expect(screen.queryByText('City 1')).not.toBeInTheDocument()
    expect(screen.getByText('MTB 2')).toBeInTheDocument()
  })

  it('disables the add-to-cart button until a range is selected', async () => {
    renderCatalog()
    const buttons = await screen.findAllByRole('button', { name: 'Kosárba' })
    expect(buttons[0]).toBeDisabled()
  })

  it('adds the bike to the cart with the selected range', async () => {
    renderCatalog()
    await screen.findByText('MTB 2')
    fireEvent.click(screen.getByRole('button', { name: 'mock-select-range' }))
    fireEvent.click(screen.getAllByRole('button', { name: 'Kosárba' })[0])

    await waitFor(() => expect(screen.getByText('MTB 2 hozzáadva a kosárhoz.')).toBeInTheDocument())
    expect(JSON.parse(localStorage.getItem('rentalCart'))).toEqual([
      { bikeId: 2, name: 'MTB 2', dailyPrice: 200, startDate: '2030-05-10', endDate: '2030-05-12' }
    ])
    expect(screen.getByRole('button', { name: 'Kosár (1)' })).toBeInTheDocument()
  })

  it('shows an error when loading fails', async () => {
    getBikes.mockRejectedValue(new Error('Hálózati hiba'))
    renderCatalog()
    expect(await screen.findByText('Hiba: Hálózati hiba')).toBeInTheDocument()
  })
})
