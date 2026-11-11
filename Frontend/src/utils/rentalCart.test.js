import { beforeEach, describe, expect, it } from 'vitest'
import { addToCart, clearCart, loadCart, removeFromCart } from './rentalCart'

const bike = { id: 1, name: 'City 1', dailyPrice: 1000 }
const otherBike = { id: 2, name: 'MTB 2', dailyPrice: 2000 }

describe('rentalCart', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('returns an empty cart when nothing is stored', () => {
    expect(loadCart()).toEqual([])
  })

  it('returns an empty cart when stored data is invalid JSON', () => {
    localStorage.setItem('rentalCart', '{broken')
    expect(loadCart()).toEqual([])
  })

  it('adds a bike with its dates', () => {
    addToCart(bike, '2030-05-10', '2030-05-12')
    expect(loadCart()).toEqual([
      { bikeId: 1, name: 'City 1', dailyPrice: 1000, startDate: '2030-05-10', endDate: '2030-05-12' }
    ])
  })

  it('replaces the existing item when the same bike is added again', () => {
    addToCart(bike, '2030-05-10', '2030-05-12')
    addToCart(bike, '2030-06-01', '2030-06-03')
    const cart = loadCart()
    expect(cart).toHaveLength(1)
    expect(cart[0].startDate).toBe('2030-06-01')
  })

  it('keeps multiple bikes in the cart', () => {
    addToCart(bike, '2030-05-10', '2030-05-12')
    addToCart(otherBike, '2030-05-10', '2030-05-12')
    expect(loadCart().map(item => item.bikeId)).toEqual([1, 2])
  })

  it('removes an item by index', () => {
    addToCart(bike, '2030-05-10', '2030-05-12')
    addToCart(otherBike, '2030-05-10', '2030-05-12')
    removeFromCart(0)
    expect(loadCart().map(item => item.bikeId)).toEqual([2])
  })

  it('clears the cart', () => {
    addToCart(bike, '2030-05-10', '2030-05-12')
    clearCart()
    expect(loadCart()).toEqual([])
  })
})
