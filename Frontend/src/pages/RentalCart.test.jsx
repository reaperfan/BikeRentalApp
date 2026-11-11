import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import RentalCart from './RentalCart'
import { createRental } from '../api/bikeService'
import { useAuth } from '../auth/AuthContext'

vi.mock('../api/bikeService', () => ({
  createRental: vi.fn()
}))

vi.mock('../auth/AuthContext', () => ({
  useAuth: vi.fn()
}))

const cartItem = {
  bikeId: 1,
  name: 'City 1',
  dailyPrice: 100,
  startDate: '2030-05-10',
  endDate: '2030-05-12'
}

function renderCart() {
  return render(
    <MemoryRouter>
      <RentalCart />
    </MemoryRouter>
  )
}

describe('RentalCart', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
    useAuth.mockReturnValue({ user: { username: 'anna', email: 'anna@example.com' } })
  })

  it('shows an empty state when the cart has no items', () => {
    renderCart()
    expect(screen.getByText('A kosár üres.')).toBeInTheDocument()
  })

  it('lists cart items with the nights and the total price', () => {
    localStorage.setItem('rentalCart', JSON.stringify([cartItem]))
    renderCart()
    expect(screen.getByText('City 1')).toBeInTheDocument()
    expect(screen.getByText('2 nap × 100 Ft/nap')).toBeInTheDocument()
    expect(screen.getByText(/Összesen:/).closest('p')).toHaveTextContent('200 Ft')
  })

  it('removes an item from the cart', () => {
    localStorage.setItem('rentalCart', JSON.stringify([cartItem]))
    renderCart()
    fireEvent.click(screen.getByRole('button', { name: 'Eltávolítás' }))
    expect(screen.getByText('A kosár üres.')).toBeInTheDocument()
    expect(JSON.parse(localStorage.getItem('rentalCart'))).toEqual([])
  })

  it('creates one rental per item using the logged-in user', async () => {
    localStorage.setItem(
      'rentalCart',
      JSON.stringify([cartItem, { ...cartItem, bikeId: 2, name: 'MTB 2' }])
    )
    createRental.mockResolvedValue({})
    renderCart()

    fireEvent.click(screen.getByRole('button', { name: 'Foglalás megerősítése' }))

    await waitFor(() => expect(createRental).toHaveBeenCalledTimes(2))
    expect(createRental).toHaveBeenNthCalledWith(1, {
      bikeId: 1,
      customerName: 'anna',
      customerEmail: 'anna@example.com',
      startDate: '2030-05-10',
      endDate: '2030-05-12'
    })
    expect(createRental).toHaveBeenNthCalledWith(2, expect.objectContaining({ bikeId: 2 }))
    expect(await screen.findByText(/sikeresen létrehozva/)).toBeInTheDocument()
    expect(JSON.parse(localStorage.getItem('rentalCart'))).toEqual([])
  })

  it('shows an error and keeps the remaining items when a rental fails', async () => {
    localStorage.setItem('rentalCart', JSON.stringify([cartItem]))
    createRental.mockRejectedValue(new Error('Ütközik egy foglalással'))
    renderCart()

    fireEvent.click(screen.getByRole('button', { name: 'Foglalás megerősítése' }))

    expect(await screen.findByText(/A foglalás sikertelen: Ütközik egy foglalással/)).toBeInTheDocument()
    expect(JSON.parse(localStorage.getItem('rentalCart'))).toHaveLength(1)
  })
})
