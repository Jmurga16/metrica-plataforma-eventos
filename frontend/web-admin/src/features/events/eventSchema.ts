import { z } from 'zod'

const zoneSchema = z.object({
  name: z.string().trim().min(1, 'El nombre de la zona es obligatorio.').max(100, 'Máximo 100 caracteres.'),
  price: z.number({ error: 'Ingresa un precio válido.' }).min(0, 'El precio debe ser mayor o igual a 0.').refine(
    (value) => Number.isInteger(value * 100),
    'El precio admite como máximo 2 decimales.',
  ),
  capacity: z.number({ error: 'Ingresa una capacidad válida.' }).int('La capacidad debe ser un número entero.').min(1, 'La capacidad debe ser mayor a 0.').max(100000, 'La capacidad máxima es 100000.'),
})

export const eventSchema = z.object({
  name: z.string().trim().min(3, 'El nombre es obligatorio (3 a 150 caracteres).').max(150, 'El nombre es obligatorio (3 a 150 caracteres).'),
  date: z.string().min(1, 'La fecha es obligatoria.').refine(
    (value) => value !== '' && new Date(value).getTime() > Date.now(),
    'La fecha debe ser futura.',
  ),
  venue: z.string().trim().min(3, 'El lugar es obligatorio.').max(200, 'El lugar admite máximo 200 caracteres.'),
  zones: z.array(zoneSchema).min(1, 'Agrega al menos una zona.').max(20, 'Puedes agregar hasta 20 zonas.'),
}).superRefine(({ zones }, context) => {
  const names = new Set<string>()
  zones.forEach((zone, index) => {
    const normalized = zone.name.trim().toLocaleLowerCase()
    if (normalized && names.has(normalized)) {
      context.addIssue({ code: 'custom', path: ['zones', index, 'name'], message: 'Zona duplicada.' })
    }
    names.add(normalized)
  })
})

export type EventFormValues = z.infer<typeof eventSchema>

export const emptyEventForm: EventFormValues = {
  name: '',
  date: '',
  venue: '',
  zones: [{ name: '', price: 0, capacity: 1 }],
}
