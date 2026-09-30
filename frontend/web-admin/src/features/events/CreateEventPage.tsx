import { zodResolver } from '@hookform/resolvers/zod'
import { AlertCircle, CheckCircle2, LoaderCircle, Save } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError } from '../../api/httpClient'
import { createEvent } from '../../api/eventsApi'
import { useDemoAuth } from '../../auth/DemoAuthProvider'
import { emptyEventForm, eventSchema, type EventFormValues } from './eventSchema'
import { ZonesFieldArray } from './ZonesFieldArray'

interface Notice { kind: 'success' | 'error'; message: string }

export function CreateEventPage() {
  const { token, role } = useDemoAuth()
  const [notice, setNotice] = useState<Notice | null>(null)
  const form = useForm<EventFormValues>({
    resolver: zodResolver(eventSchema),
    defaultValues: emptyEventForm,
    mode: 'onSubmit',
    shouldFocusError: true,
  })

  async function onSubmit(values: EventFormValues) {
    setNotice(null)
    if (!token) {
      setNotice({ kind: 'error', message: 'Selecciona el rol Admin para iniciar una sesión de demostración.' })
      return
    }

    try {
      await createEvent({
        ...values,
        name: values.name.trim(),
        venue: values.venue.trim(),
        date: new Date(values.date).toISOString(),
        zones: values.zones.map((zone) => ({ ...zone, name: zone.name.trim() })),
      }, token)
      setNotice({ kind: 'success', message: 'Evento registrado correctamente.' })
      form.reset(emptyEventForm)
    } catch (error) {
      handleApiError(error, form.setError, setNotice)
    }
  }

  const { errors, isSubmitting } = form.formState

  return (
    <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_270px]">
      <section>
        <div className="mb-7">
          <p className="mb-2 text-sm font-semibold uppercase tracking-widest text-indigo-600">Nuevo registro</p>
          <h1 className="text-3xl font-bold tracking-tight text-slate-950">Registrar evento</h1>
          <p className="mt-2 max-w-2xl text-slate-600">Completa la información general y configura las zonas disponibles.</p>
        </div>

        {notice && (
          <div role="alert" className={`mb-5 flex items-start gap-3 rounded-xl border p-4 text-sm ${notice.kind === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-red-200 bg-red-50 text-red-800'}`}>
            {notice.kind === 'success' ? <CheckCircle2 aria-hidden="true" size={19} /> : <AlertCircle aria-hidden="true" size={19} />}
            <span>{notice.message}</span>
          </div>
        )}

        <form className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-7" onSubmit={form.handleSubmit(onSubmit)} noValidate>
          <div className="grid gap-5 sm:grid-cols-2">
            <label className="sm:col-span-2">
              <span className="field-label">Nombre del evento</span>
              <input className="field-input" placeholder="Ej. Concierto de verano" aria-invalid={!!errors.name} {...form.register('name')} />
              {errors.name && <span className="field-error block" role="alert">{errors.name.message}</span>}
            </label>
            <label>
              <span className="field-label">Fecha y hora</span>
              <input type="datetime-local" className="field-input" aria-invalid={!!errors.date} {...form.register('date')} />
              {errors.date && <span className="field-error block" role="alert">{errors.date.message}</span>}
            </label>
            <label>
              <span className="field-label">Lugar</span>
              <input className="field-input" placeholder="Ej. Gran Teatro Nacional" aria-invalid={!!errors.venue} {...form.register('venue')} />
              {errors.venue && <span className="field-error block" role="alert">{errors.venue.message}</span>}
            </label>
          </div>

          <ZonesFieldArray form={form} />

          <div className="mt-8 flex flex-col-reverse items-stretch justify-between gap-3 border-t border-slate-200 pt-6 sm:flex-row sm:items-center">
            <p className="text-sm text-slate-500">Los campos son obligatorios.</p>
            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex min-w-44 items-center justify-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-indigo-400"
            >
              {isSubmitting ? <LoaderCircle aria-hidden="true" className="animate-spin" size={18} /> : <Save aria-hidden="true" size={18} />}
              {isSubmitting ? 'Registrando…' : 'Registrar evento'}
            </button>
          </div>
        </form>
      </section>

      <aside className="h-fit rounded-2xl border border-slate-200 bg-white p-5 shadow-sm lg:mt-24">
        <h2 className="font-semibold text-slate-900">Sesión actual</h2>
        <div className="mt-3 flex items-center gap-2">
          <span className={`size-2 rounded-full ${token ? 'bg-emerald-500' : 'bg-amber-500'}`} />
          <span className="text-sm text-slate-600">{token ? `Rol activo: ${role}` : 'Sin sesión iniciada'}</span>
        </div>
        <p className="mt-4 text-sm leading-6 text-slate-500">Solo el rol Admin puede registrar eventos. El token se conserva únicamente en memoria.</p>
      </aside>
    </div>
  )
}

type SetFieldError = ReturnType<typeof useForm<EventFormValues>>['setError']

function handleApiError(error: unknown, setError: SetFieldError, setNotice: (notice: Notice) => void) {
  if (!(error instanceof ApiError)) {
    setNotice({ kind: 'error', message: 'Ocurrió un error inesperado. Intenta nuevamente.' })
    return
  }

  if (error.status === 400 && error.problem.errors) {
    for (const [rawPath, messages] of Object.entries(error.problem.errors)) {
      const path = normalizeFieldPath(rawPath)
      if (isFormPath(path)) setError(path, { type: 'server', message: messages[0] })
    }
    setNotice({ kind: 'error', message: 'Revisa los campos marcados e intenta nuevamente.' })
    return
  }

  const trace = error.problem.traceId ? ` Código de soporte: ${error.problem.traceId}` : ''
  const messages: Record<number, string> = {
    0: `No pudimos conectar con el servidor. Intenta nuevamente.${trace}`,
    401: 'Tu sesión expiró. Selecciona nuevamente un rol de demo.',
    403: 'No tienes permisos para registrar eventos (rol requerido: Admin).',
    429: `Demasiadas solicitudes, intenta en ${error.retryAfter ?? 'unos'} segundos.`,
  }
  setNotice({ kind: 'error', message: messages[error.status] ?? `No pudimos registrar el evento.${trace}` })
}

function normalizeFieldPath(path: string) {
  const normalized = path.replace(/^\$?\.?/, '').replace(/\[(\d+)\]/g, '.$1')
  return normalized.split('.').map((part) => part ? part[0].toLowerCase() + part.slice(1) : part).join('.')
}

function isFormPath(path: string): path is keyof EventFormValues | `zones.${number}.${'name' | 'price' | 'capacity'}` {
  return /^(name|date|venue|zones|zones\.\d+\.(name|price|capacity))$/.test(path)
}
