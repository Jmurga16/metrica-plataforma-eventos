const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5001').replace(/\/$/, '')

export interface ProblemDetails {
  title?: string
  status?: number
  traceId?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails,
    readonly retryAfter?: string,
  ) {
    super(problem.title ?? 'La solicitud no pudo completarse.')
  }
}

interface RequestOptions extends RequestInit {
  token?: string
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  headers.set('X-Correlation-Id', crypto.randomUUID())
  if (options.body) headers.set('Content-Type', 'application/json')
  if (options.token) headers.set('Authorization', `Bearer ${options.token}`)

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers })
  } catch {
    throw new ApiError(0, { title: 'No pudimos conectar con el servidor.' })
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as ProblemDetails
    throw new ApiError(response.status, problem, response.headers.get('Retry-After') ?? undefined)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
