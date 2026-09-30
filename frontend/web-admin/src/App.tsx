import { CalendarDays, List, Plus } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { DemoRoleSelector } from './auth/DemoRoleSelector'
import { CreateEventPage } from './features/events/CreateEventPage'
import { EventsPage } from './features/events/EventsPage'

type Page = 'events' | 'create'

export function App() {
  const [page, setPage] = useState<Page>('events')

  return (
    <div className="min-h-screen bg-slate-50 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-5 py-4 sm:px-8">
          <div className="flex items-center gap-3">
            <span className="grid size-10 place-items-center rounded-xl bg-indigo-600 text-white shadow-sm">
              <CalendarDays aria-hidden="true" size={21} />
            </span>
            <div>
              <p className="font-semibold tracking-tight">Métrica Eventos</p>
              <p className="text-xs text-slate-500">Panel de administración</p>
            </div>
          </div>
          <div className="flex flex-wrap items-center justify-end gap-3 sm:gap-5">
            <nav aria-label="Navegación principal" className="flex rounded-lg bg-slate-100 p-1">
              <NavButton active={page === 'events'} icon={<List aria-hidden="true" size={16} />} onClick={() => setPage('events')}>
                Eventos
              </NavButton>
              <NavButton active={page === 'create'} icon={<Plus aria-hidden="true" size={16} />} onClick={() => setPage('create')}>
                Registrar
              </NavButton>
            </nav>
            <DemoRoleSelector />
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-5 py-8 sm:px-8 sm:py-12">
        {page === 'events' ? <EventsPage /> : <CreateEventPage />}
      </main>
    </div>
  )
}

function NavButton({ active, icon, onClick, children }: {
  active: boolean
  icon: ReactNode
  onClick: () => void
  children: ReactNode
}) {
  return (
    <button
      type="button"
      aria-current={active ? 'page' : undefined}
      onClick={onClick}
      className={`inline-flex items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium transition ${active ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-600 hover:text-slate-950'}`}
    >
      {icon}
      {children}
    </button>
  )
}
