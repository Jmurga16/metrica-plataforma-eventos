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

export interface EventZone {
  id: string
  name: string
  price: number
  capacity: number
}

export interface EventSummary {
  id: string
  name: string
  date: string
  venue: string
  status: string
  zones: EventZone[]
}

export function createEvent(payload: CreateEventRequest, token: string) {
  return apiRequest<CreatedEvent>('/events', {
    method: 'POST',
    token,
    body: JSON.stringify(payload),
  })
}

export function getEvents(token: string, page = 1, pageSize = 20) {
  return apiRequest<EventSummary[]>(`/events?page=${page}&pageSize=${pageSize}`, { token })
}

export function getEvent(id: string, token: string) {
  return apiRequest<EventSummary>(`/events/${encodeURIComponent(id)}`, { token })
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
