import { api } from './client'

export async function getBikes(filters = {}) {
  const params = new URLSearchParams()
  if (filters.type !== undefined) params.append('type', filters.type)
  if (filters.available !== undefined) params.append('available', filters.available)

  const queryString = params.toString()
  const path = queryString ? `/bikes?${queryString}` : '/bikes'
  return api(path, { auth: false })
}

export async function getBikeById(id) {
  return api(`/bikes/${id}`, { auth: false })
}

export async function createRental(rentalData) {
  return api('/rentals', {
    method: 'POST',
    body: rentalData
  })
}

export async function getRentals() {
  return api('/rentals', { auth: false })
}

export async function getRentalById(id) {
  return api(`/rentals/${id}`, { auth: false })
}

export async function getRentalsByBike(bikeId) {
  return api(`/rentals/bike/${bikeId}`, { auth: false })
}

export async function updateRental(id, rentalData) {
  return api(`/rentals/${id}`, {
    method: 'PUT',
    body: rentalData,
    auth: false
  })
}

export async function deleteRental(id) {
  return api(`/rentals/${id}`, {
    method: 'DELETE',
    auth: false
  })
}
