import { api } from './client'

export const getStats = () => api('/admin/stats')

export const getUsers = () => api('/admin/users')
export const updateUserRole = (id, role) => api(`/admin/users/${id}/role`, { method: 'PATCH', body: { role } })

export function getAdminRentals(filters = {}) {
  const params = new URLSearchParams()
  if (filters.bikeId) params.append('bikeId', filters.bikeId)
  if (filters.from) params.append('from', filters.from)
  if (filters.to) params.append('to', filters.to)
  const query = params.toString()
  return api(`/admin/rentals${query ? `?${query}` : ''}`)
}
export const deleteAdminRental = (id) => api(`/admin/rentals/${id}`, { method: 'DELETE' })

export const createBike = (bike) => api('/bikes', { method: 'POST', body: bike })
export const updateBike = (id, bike) => api(`/bikes/${id}`, { method: 'PUT', body: bike })
export const deleteBike = (id) => api(`/bikes/${id}`, { method: 'DELETE' })

export const getServiceAppointments = (from, to) => api(`/service-appointments/admin?from=${from}&to=${to}`)
export const updateServiceStatus = (id, status) =>
  api(`/service-appointments/admin/${id}/status`, { method: 'PATCH', body: { status } })
