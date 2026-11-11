const CART_KEY = 'rentalCart'

export function loadCart() {
  try {
    return JSON.parse(localStorage.getItem(CART_KEY)) ?? []
  } catch {
    return []
  }
}

function saveCart(items) {
  try {
    localStorage.setItem(CART_KEY, JSON.stringify(items))
  } catch {
    // privát mód / tiltott storage: a kosár csak az oldal életciklusáig marad meg
  }
}

export function addToCart(bike, startDate, endDate) {
  const items = loadCart().filter(item => item.bikeId !== bike.id)
  saveCart([
    ...items,
    { bikeId: bike.id, name: bike.name, dailyPrice: bike.dailyPrice, startDate, endDate }
  ])
}

export function removeFromCart(index) {
  saveCart(loadCart().filter((_, i) => i !== index))
}

export function clearCart() {
  saveCart([])
}
