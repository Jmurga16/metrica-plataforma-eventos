import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../../App'
import { ApiError } from '../../api/httpClient'
import { createEvent, requestDemoToken } from '../../api/eventsApi'
import { DemoAuthProvider } from '../../auth/DemoAuthProvider'

vi.mock('../../api/eventsApi', () => ({
  createEvent: vi.fn(),
  requestDemoToken: vi.fn(),
}))

const mockedCreateEvent = vi.mocked(createEvent)
const mockedRequestToken = vi.mocked(requestDemoToken)

function renderApp() {
  return render(<DemoAuthProvider><App /></DemoAuthProvider>)
}

async function loginAndFill(role: 'Admin' | 'User' = 'Admin') {
  const user = userEvent.setup()
  await user.selectOptions(screen.getByLabelText('Rol de demo'), role)
  await user.click(screen.getByRole('button', { name: 'Registrar' }))
  await waitFor(() => expect(screen.getByText(`Rol activo: ${role}`)).toBeInTheDocument())
  await user.type(screen.getByLabelText('Nombre del evento'), 'Concierto de prueba')
  await user.type(screen.getByLabelText('Fecha y hora'), '2099-12-20T20:00')
  await user.type(screen.getByLabelText('Lugar'), 'Arena Lima')
  await user.type(screen.getByLabelText('Nombre de zona'), 'VIP')
  const price = screen.getByLabelText('Precio (S/)')
  await user.clear(price)
  await user.type(price, '150')
  return user
}

describe('CreateEventPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedRequestToken.mockResolvedValue('demo-token')
    mockedCreateEvent.mockResolvedValue({ id: 'evt-123' })
  })

  it('rechaza capacidad cero sin llamar a la API', async () => {
    renderApp()
    const user = await loginAndFill()
    const capacity = screen.getByLabelText('Capacidad')
    await user.clear(capacity)
    await user.type(capacity, '0')
    await user.click(screen.getByRole('button', { name: 'Registrar evento' }))

    expect(await screen.findByText('La capacidad debe ser mayor a 0.')).toBeInTheDocument()
    expect(mockedCreateEvent).not.toHaveBeenCalled()
  })

  it('mapea errores 400 a la zona correcta', async () => {
    mockedCreateEvent.mockRejectedValue(new ApiError(400, {
      title: 'Datos inválidos',
      errors: { 'zones[0].capacity': ['La capacidad excede el límite disponible.'] },
    }))
    renderApp()
    const user = await loginAndFill()
    await user.click(screen.getByRole('button', { name: 'Registrar evento' }))

    expect(await screen.findByText('La capacidad excede el límite disponible.')).toBeInTheDocument()
  })

  it('deshabilita el botón mientras envía', async () => {
    mockedCreateEvent.mockImplementation(() => new Promise(() => undefined))
    renderApp()
    const user = await loginAndFill()
    await user.click(screen.getByRole('button', { name: 'Registrar evento' }))

    expect(await screen.findByRole('button', { name: 'Registrando…' })).toBeDisabled()
  })

  it('confirma el registro sin exponer el identificador y limpia el formulario después de 201', async () => {
    renderApp()
    const user = await loginAndFill()
    await user.click(screen.getByRole('button', { name: 'Registrar evento' }))

    expect(await screen.findByText('Evento registrado correctamente.')).toBeInTheDocument()
    expect(screen.queryByText(/evt-123/)).not.toBeInTheDocument()
    expect(screen.getByLabelText('Nombre del evento')).toHaveValue('')
  })

  it('explica que Admin es requerido ante un 403', async () => {
    mockedCreateEvent.mockRejectedValue(new ApiError(403, { title: 'Forbidden' }))
    renderApp()
    const user = await loginAndFill('User')
    await user.click(screen.getByRole('button', { name: 'Registrar evento' }))

    expect(await screen.findByText('No tienes permisos para registrar eventos (rol requerido: Admin).')).toBeInTheDocument()
  })
})
