import { AlertCircle, ArrowLeft, CalendarDays, ChevronRight, LoaderCircle, MapPin, RefreshCw, Ticket } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { ApiError } from '../../api/httpClient'
import { getEvent, getEvents, type EventSummary } from '../../api/eventsApi'
import { useDemoAuth } from '../../auth/DemoAuthProvider'

export function EventsPage() {
  const { token } = useDemoAuth()
  const [events, setEvents] = useState<EventSummary[]>([])
  const [selected, setSelected] = useState<EventSummary | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadEvents = useCallback(async () => {
    if (!token) {
      setEvents([])
      setSelected(null)
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      setEvents(await getEvents(token))
    } catch (requestError) {
      setError(readErrorMessage(requestError, 'No pudimos cargar los eventos.'))
    } finally {
      setIsLoading(false)
    }
  }, [token])

  useEffect(() => {
    void loadEvents()
  }, [loadEvents])

  async function openEvent(id: string) {
    if (!token) return
    setIsLoading(true)
    setError(null)
    try {
      setSelected(await getEvent(id, token))
    } catch (requestError) {
      setError(readErrorMessage(requestError, 'No pudimos cargar el detalle del evento.'))
    } finally {
      setIsLoading(false)
    }
  }

  if (!token) {
    return (
      <EmptyState
        title="Consulta los eventos registrados"
        description="Selecciona un rol de demo en el encabezado para acceder al listado y sus detalles."
      />
    )
  }

  if (selected) {
    return <EventDetail event={selected} onBack={() => setSelected(null)} />
  }

  return (
    <section>
      <div className="mb-7 flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="mb-2 text-sm font-semibold uppercase tracking-widest text-indigo-600">Administración</p>
          <h1 className="text-3xl font-bold tracking-tight text-slate-950">Eventos registrados</h1>
          <p className="mt-2 text-slate-600">Consulta la información general y las zonas disponibles.</p>
        </div>
        <button type="button" onClick={() => void loadEvents()} disabled={isLoading} className="secondary-button">
          <RefreshCw aria-hidden="true" size={17} className={isLoading ? 'animate-spin' : ''} />
          Actualizar
        </button>
      </div>

      {error && <ErrorNotice message={error} />}

      {isLoading && events.length === 0 ? (
        <div className="flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white py-16 text-sm text-slate-500">
          <LoaderCircle aria-hidden="true" className="animate-spin" size={20} /> Cargando eventos…
        </div>
      ) : events.length === 0 && !error ? (
        <EmptyState title="Aún no hay eventos" description="Cuando registres el primer evento aparecerá en este listado." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {events.map((event) => (
            <article key={event.id} className="flex flex-col rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="mb-4 flex items-start justify-between gap-3">
                <span className="rounded-full bg-indigo-50 px-2.5 py-1 text-xs font-semibold text-indigo-700">{event.status}</span>
                <span className="text-xs text-slate-500">{event.zones.length} {event.zones.length === 1 ? 'zona' : 'zonas'}</span>
              </div>
              <h2 className="text-lg font-semibold text-slate-950">{event.name}</h2>
              <div className="mt-3 space-y-2 text-sm text-slate-600">
                <p className="flex items-center gap-2"><CalendarDays aria-hidden="true" size={16} />{formatDate(event.date)}</p>
                <p className="flex items-center gap-2"><MapPin aria-hidden="true" size={16} />{event.venue}</p>
              </div>
              <button type="button" onClick={() => void openEvent(event.id)} className="mt-5 inline-flex items-center justify-between border-t border-slate-100 pt-4 text-sm font-semibold text-indigo-700 hover:text-indigo-900">
                Ver detalle <ChevronRight aria-hidden="true" size={17} />
              </button>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

function EventDetail({ event, onBack }: { event: EventSummary; onBack: () => void }) {
  return (
    <section>
      <button type="button" onClick={onBack} className="secondary-button mb-6">
        <ArrowLeft aria-hidden="true" size={17} /> Volver a eventos
      </button>
      <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-7">
        <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-200 pb-6">
          <div>
            <span className="rounded-full bg-indigo-50 px-2.5 py-1 text-xs font-semibold text-indigo-700">{event.status}</span>
            <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-950">{event.name}</h1>
            <div className="mt-3 flex flex-wrap gap-x-6 gap-y-2 text-sm text-slate-600">
              <p className="flex items-center gap-2"><CalendarDays aria-hidden="true" size={16} />{formatDate(event.date)}</p>
              <p className="flex items-center gap-2"><MapPin aria-hidden="true" size={16} />{event.venue}</p>
            </div>
          </div>
        </div>
        <h2 className="mt-6 text-lg font-semibold text-slate-950">Zonas disponibles</h2>
        <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {event.zones.map((zone) => (
            <article key={zone.id} className="rounded-xl border border-slate-200 p-4">
              <div className="flex items-center gap-2 font-semibold text-slate-900"><Ticket aria-hidden="true" size={17} className="text-indigo-600" />{zone.name}</div>
              <p className="mt-3 text-2xl font-bold text-slate-950">{formatPrice(zone.price)}</p>
              <p className="mt-1 text-sm text-slate-500">Capacidad: {zone.capacity.toLocaleString('es-PE')}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
      <span className="mx-auto grid size-12 place-items-center rounded-full bg-indigo-50 text-indigo-600"><CalendarDays aria-hidden="true" /></span>
      <h1 className="mt-4 text-xl font-semibold text-slate-950">{title}</h1>
      <p className="mx-auto mt-2 max-w-lg text-sm text-slate-600">{description}</p>
    </div>
  )
}

function ErrorNotice({ message }: { message: string }) {
  return <div role="alert" className="mb-5 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"><AlertCircle aria-hidden="true" size={19} />{message}</div>
}

function readErrorMessage(error: unknown, fallback: string) {
  if (!(error instanceof ApiError)) return fallback
  if (error.status === 401) return 'Tu sesión expiró. Selecciona nuevamente un rol de demo.'
  if (error.status === 403) return 'No tienes permisos para consultar eventos.'
  if (error.status === 404) return 'El evento solicitado ya no está disponible.'
  const trace = error.problem.traceId ? ` Código de soporte: ${error.problem.traceId}` : ''
  return `${fallback}${trace}`
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('es-PE', { dateStyle: 'long', timeStyle: 'short' }).format(new Date(value))
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' }).format(value)
}
