import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Admin from './Admin'
import AdminRoute from '../auth/AdminRoute'
import * as adminService from '../api/adminService'
import { getBikes } from '../api/bikeService'
import { useAuth } from '../auth/AuthContext'

vi.mock('../api/adminService', () => ({
  getStats: vi.fn(),
  getUsers: vi.fn(),
  updateUserRole: vi.fn(),
  getAdminRentals: vi.fn(),
  deleteAdminRental: vi.fn(),
  createBike: vi.fn(),
  updateBike: vi.fn(),
  deleteBike: vi.fn(),
  uploadBikeImage: vi.fn(),
  getServiceAppointments: vi.fn(),
  updateServiceStatus: vi.fn()
}))

vi.mock('../api/bikeService', () => ({ getBikes: vi.fn() }))
vi.mock('../auth/AuthContext', () => ({ useAuth: vi.fn() }))

const admin = { id: 1, username: 'boss', email: 'boss@example.com', role: 'Admin' }
const bikes = [
  { id: 1, name: 'Trek Marlin 7', type: 0, dailyPrice: 8000, imageUrl: '/a.jpg', isAvailable: true },
  { id: 2, name: 'Csepel Royal', type: 2, dailyPrice: 4500, imageUrl: '/b.jpg', isAvailable: false }
]
const users = [admin, { id: 2, username: 'anna', email: 'anna@example.com', role: 'Customer', createdAt: '2026-11-01T10:00:00Z' }]
users[0].createdAt = '2026-10-01T10:00:00Z'

function renderAdmin(path = '/admin') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/admin" element={<AdminRoute><Admin /></AdminRoute>} />
        <Route path="/" element={<p>főoldal</p>} />
        <Route path="/login" element={<p>login oldal</p>} />
      </Routes>
    </MemoryRouter>
  )
}

describe('Admin', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    useAuth.mockReturnValue({ user: admin, loading: false, logout: vi.fn() })
    getBikes.mockResolvedValue(bikes)
    adminService.getUsers.mockResolvedValue(users)
    adminService.getStats.mockResolvedValue({
      bikeCount: 15, availableBikeCount: 14, userCount: 5, adminCount: 1, rentalCount: 7, activeRentalCount: 2,
      upcomingRentalCount: 3, totalRevenue: 123000, pendingServiceAppointmentCount: 4, reviewCount: 2, averageRating: 4.5
    })
  })

  it('redirects non-admin users to the home page', () => {
    useAuth.mockReturnValue({ user: { ...admin, role: 'Customer' }, loading: false })
    renderAdmin()
    expect(screen.getByText('főoldal')).toBeInTheDocument()
  })

  it('redirects anonymous users to login', () => {
    useAuth.mockReturnValue({ user: null, loading: false })
    renderAdmin()
    expect(screen.getByText('login oldal')).toBeInTheDocument()
  })

  it('shows the overview stats and navigates to a tab', async () => {
    renderAdmin()
    expect(await screen.findByText('123 000 Ft')).toBeInTheDocument()
    expect(screen.getByText('4.5 / 5 átlag')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Felhasználók/ }))
    expect(await screen.findByText('anna@example.com')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Felhasználók' })).toHaveAttribute('aria-selected', 'true')
  })

  it('creates a new bike from the form', async () => {
    adminService.createBike.mockResolvedValue({})
    renderAdmin('/admin?tab=bikes')
    await screen.findByText('Trek Marlin 7')

    fireEvent.change(screen.getByLabelText('Név'), { target: { value: '  Cube Test  ' } })
    fireEvent.change(screen.getByLabelText('Típus'), { target: { value: '3' } })
    fireEvent.change(screen.getByLabelText('Napidíj (Ft)'), { target: { value: '12000' } })
    fireEvent.change(screen.getByLabelText(/meglévő kép útvonala/), { target: { value: '/images/bikes/x.jpg' } })
    fireEvent.click(screen.getByRole('button', { name: /Bicikli felvétele/ }))

    await waitFor(() => expect(adminService.createBike).toHaveBeenCalledWith(
      { name: 'Cube Test', type: 3, dailyPrice: 12000, imageUrl: '/images/bikes/x.jpg' }))
    expect(await screen.findByText('Új bicikli felvéve.')).toBeInTheDocument()
    expect(getBikes).toHaveBeenCalledTimes(2)
  })

  it('uploads a browsed image and uses its url for the new bike', async () => {
    adminService.uploadBikeImage.mockResolvedValue({ url: '/uploads/bikes/abc.png' })
    adminService.createBike.mockResolvedValue({})
    renderAdmin('/admin?tab=bikes')
    await screen.findByText('Trek Marlin 7')

    const file = new File(['png'], 'bringa.png', { type: 'image/png' })
    fireEvent.change(screen.getByLabelText('Kép tallózása…'), { target: { files: [file] } })

    expect(await screen.findByText('bringa.png feltöltve.')).toBeInTheDocument()
    expect(adminService.uploadBikeImage).toHaveBeenCalledWith(file)
    expect(screen.getByLabelText(/meglévő kép útvonala/)).toHaveValue('/uploads/bikes/abc.png')

    fireEvent.change(screen.getByLabelText('Név'), { target: { value: 'Új bringa' } })
    fireEvent.change(screen.getByLabelText('Napidíj (Ft)'), { target: { value: '5000' } })
    fireEvent.click(screen.getByRole('button', { name: /Bicikli felvétele/ }))
    await waitFor(() => expect(adminService.createBike).toHaveBeenCalledWith(
      { name: 'Új bringa', type: 0, dailyPrice: 5000, imageUrl: '/uploads/bikes/abc.png' }))
  })

  it('rejects non-image files before uploading', async () => {
    renderAdmin('/admin?tab=bikes')
    await screen.findByText('Trek Marlin 7')

    const file = new File(['x'], 'doksi.pdf', { type: 'application/pdf' })
    fireEvent.change(screen.getByLabelText('Kép tallózása…'), { target: { files: [file] } })

    expect(await screen.findByRole('alert')).toHaveTextContent('Csak JPG, PNG vagy WEBP')
    expect(adminService.uploadBikeImage).not.toHaveBeenCalled()
  })

  it('toggles bike availability with the full bike payload', async () => {
    adminService.updateBike.mockResolvedValue({})
    renderAdmin('/admin?tab=bikes')
    const row = (await screen.findByText('Csepel Royal')).closest('tr')

    fireEvent.click(within(row).getByRole('button', { name: 'Engedélyezés' }))
    await waitFor(() => expect(adminService.updateBike).toHaveBeenCalledWith(2, { ...bikes[1], isAvailable: true }))
  })

  it('shows the backend error when a bike cannot be deleted', async () => {
    adminService.deleteBike.mockRejectedValue(new Error('A biciklihez kölcsönzés tartozik, nem törölhető.'))
    renderAdmin('/admin?tab=bikes')
    const row = (await screen.findByText('Trek Marlin 7')).closest('tr')

    fireEvent.click(within(row).getByRole('button', { name: 'Törlés' }))
    fireEvent.click(within(row).getByRole('button', { name: 'Igen, törlöm' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('nem törölhető')
  })

  it('changes another user role but not the own one', async () => {
    adminService.updateUserRole.mockResolvedValue({})
    renderAdmin('/admin?tab=users')
    const own = (await screen.findByText('boss (te)')).closest('tr')
    expect(within(own).queryByRole('button')).toBeNull()

    const anna = screen.getByText('anna').closest('tr')
    fireEvent.click(within(anna).getByRole('button', { name: 'Admin jog megadása' }))
    await waitFor(() => expect(adminService.updateUserRole).toHaveBeenCalledWith(2, 'Admin'))
  })

  it('lists rentals with totals and deletes after confirmation', async () => {
    adminService.getAdminRentals.mockResolvedValue([
      { id: 9, bikeId: 1, bikeName: 'Trek Marlin 7', customerName: 'Elek', customerEmail: 'elek@example.com',
        startDate: '2030-05-10T00:00:00', endDate: '2030-05-13T00:00:00', days: 3, totalPrice: 24000 }
    ])
    adminService.deleteAdminRental.mockResolvedValue(null)
    renderAdmin('/admin?tab=rentals')

    expect(await screen.findByText('(1 db · 24 000 Ft)')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Törlés' }))
    fireEvent.click(screen.getByRole('button', { name: 'Igen, törlöm' }))
    await waitFor(() => expect(adminService.deleteAdminRental).toHaveBeenCalledWith(9))
  })

  it('offers only the allowed service status transitions', async () => {
    adminService.getServiceAppointments.mockResolvedValue([
      { userId: 2, appointment: { id: 5, appointmentDate: '2030-05-10T08:00:00Z', bikeType: 'MTB', serviceType: 'Fék', status: 'Pending' } },
      { userId: 2, appointment: { id: 6, appointmentDate: '2030-05-11T08:00:00Z', bikeType: 'City', serviceType: 'Lánc', status: 'Completed' } }
    ])
    adminService.updateServiceStatus.mockResolvedValue(null)
    renderAdmin('/admin?tab=service')

    expect(await screen.findAllByText('anna · anna@example.com')).toHaveLength(2)
    expect(screen.getAllByRole('button', { name: 'Visszaigazolás' })).toHaveLength(1)
    fireEvent.click(screen.getByRole('button', { name: 'Visszaigazolás' }))
    await waitFor(() => expect(adminService.updateServiceStatus).toHaveBeenCalledWith(5, 'Confirmed'))
  })
})
