import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../../App'
import { getEvent, getEvents, requestDemoToken } from '../../api/eventsApi'
import { DemoAuthProvider } from '../../auth/DemoAuthProvider'

vi.mock('../../api/eventsApi', () => ({
  createEvent: vi.fn(),
  getEvent: vi.fn(),
  getEvents: vi.fn(),
  requestDemoToken: vi.fn(),
}))

const event = {
  id: '10d426a7-02b1-4fef-9d17-0ca3c09f5ac4',
  name: 'Concierto de prueba',
  date: '2099-12-20T20:00:00Z',
  venue: 'Arena Lima',
  status: 'Published',
  zones: [{ id: 'zone-1', name: 'VIP', price: 150, capacity: 100 }],
}

describe('EventsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(requestDemoToken).mockResolvedValue('demo-token')
    vi.mocked(getEvents).mockResolvedValue([event])
    vi.mocked(getEvent).mockResolvedValue(event)
  })

  it('lista eventos y consulta el detalle sin mostrar identificadores', async () => {
    render(<DemoAuthProvider><App /></DemoAuthProvider>)
    const user = userEvent.setup()

    expect(screen.getByText('Consulta los eventos registrados')).toBeInTheDocument()
    await user.selectOptions(screen.getByLabelText('Rol de demo'), 'User')

    expect(await screen.findByText('Concierto de prueba')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Ver detalle' }))

    await waitFor(() => expect(getEvent).toHaveBeenCalledWith(event.id, 'demo-token'))
    expect(screen.getByText('Zonas disponibles')).toBeInTheDocument()
    expect(screen.getByText(/S\/\s*150/)).toBeInTheDocument()
    expect(screen.queryByText(event.id)).not.toBeInTheDocument()
  })
})
