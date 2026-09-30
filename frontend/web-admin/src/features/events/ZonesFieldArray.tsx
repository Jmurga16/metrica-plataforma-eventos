import { Plus, Trash2 } from 'lucide-react'
import { useFieldArray, type UseFormReturn } from 'react-hook-form'
import type { EventFormValues } from './eventSchema'

export function ZonesFieldArray({ form }: { form: UseFormReturn<EventFormValues> }) {
  const { fields, append, remove } = useFieldArray({ control: form.control, name: 'zones' })
  const errors = form.formState.errors.zones

  return (
    <fieldset className="mt-8 border-t border-slate-200 pt-7">
      <div className="mb-4 flex items-center justify-between gap-4">
        <div>
          <legend className="text-base font-semibold text-slate-900">Zonas y capacidad</legend>
          <p className="mt-1 text-sm text-slate-500">Define precio y aforo para cada zona.</p>
        </div>
        <button
          type="button"
          className="inline-flex items-center gap-2 rounded-lg border border-indigo-200 bg-indigo-50 px-3 py-2 text-sm font-semibold text-indigo-700 hover:bg-indigo-100 disabled:opacity-50"
          disabled={fields.length >= 20}
          onClick={() => append({ name: '', price: 0, capacity: 1 })}
        >
          <Plus aria-hidden="true" size={16} /> Agregar zona
        </button>
      </div>

      <div className="space-y-4">
        {fields.map((field, index) => {
          const zoneError = errors?.[index]
          return (
            <div key={field.id} className="rounded-xl border border-slate-200 bg-slate-50/70 p-4">
              <div className="mb-3 flex items-center justify-between">
                <p className="text-sm font-semibold text-slate-700">Zona {index + 1}</p>
                <button
                  type="button"
                  className="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-sm font-medium text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-40"
                  disabled={fields.length === 1}
                  aria-label={`Eliminar zona ${index + 1}`}
                  onClick={() => remove(index)}
                >
                  <Trash2 aria-hidden="true" size={15} /> Eliminar
                </button>
              </div>
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-[1.5fr_1fr_1fr]">
                <Field label="Nombre de zona" error={zoneError?.name?.message}>
                  <input className="field-input" aria-invalid={!!zoneError?.name} {...form.register(`zones.${index}.name`)} />
                </Field>
                <Field label="Precio (S/)" error={zoneError?.price?.message}>
                  <input type="number" min="0" step="0.01" className="field-input" aria-invalid={!!zoneError?.price} {...form.register(`zones.${index}.price`, { valueAsNumber: true })} />
                </Field>
                <Field label="Capacidad" error={zoneError?.capacity?.message}>
                  <input type="number" min="1" step="1" className="field-input" aria-invalid={!!zoneError?.capacity} {...form.register(`zones.${index}.capacity`, { valueAsNumber: true })} />
                </Field>
              </div>
            </div>
          )
        })}
      </div>
      {errors?.root?.message && <p className="field-error" role="alert">{errors.root.message}</p>}
    </fieldset>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactElement }) {
  return (
    <label>
      <span className="field-label">{label}</span>
      {children}
      {error && <span className="field-error block" role="alert">{error}</span>}
    </label>
  )
}
