const TOKEN_KEY = 'token'

export function getToken() {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function setToken(token) {
  try {
    if (token) localStorage.setItem(TOKEN_KEY, token)
    else localStorage.removeItem(TOKEN_KEY)
  } catch {
    // privát mód / tiltott storage: csak memóriában marad a login
  }
}

export class ApiError extends Error {
  constructor(status, message) {
    super(message)
    this.status = status
  }
}

// fetch wrapper: JSON body, Bearer token, egységes hibakezelés
export async function api(path, { method = 'GET', body, auth = true } = {}) {
  const headers = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  const token = getToken()
  if (auth && token) headers.Authorization = `Bearer ${token}`

  let res
  try {
    res = await fetch(`/api${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'A szerver nem érhető el.')
  }

  const data = await res.json().catch(() => null)
  if (!res.ok) throw new ApiError(res.status, data?.message ?? `Hiba történt (${res.status}).`)
  return data
}
