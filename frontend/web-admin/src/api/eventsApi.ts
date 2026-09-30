import { apiRequest } from './httpClient'

export interface CreateEventRequest {
  name: string
  date: string
  venue: string
  zones: Array<{ name: string; price: number; capacity: number }>
}

export interface CreatedEvent {
  id: string
  name?: string
}

export function createEvent(payload: CreateEventRequest, token: string) {
  return apiRequest<CreatedEvent>('/events', {
    method: 'POST',
    token,
    body: JSON.stringify(payload),
  })
}

export async function requestDemoToken(role: 'Admin' | 'User') {
  const response = await apiRequest<{ token?: string; accessToken?: string }>('/auth/dev-token', {
    method: 'POST',
    body: JSON.stringify({ role }),
  })
  const token = response.accessToken ?? response.token
  if (!token) throw new Error('La API no devolvió un token de acceso.')
  return token
}
