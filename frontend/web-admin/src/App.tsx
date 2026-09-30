import { CalendarDays } from 'lucide-react'
import { DemoRoleSelector } from './auth/DemoRoleSelector'
import { CreateEventPage } from './features/events/CreateEventPage'

export function App() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-5 px-5 py-4 sm:px-8">
          <div className="flex items-center gap-3">
            <span className="grid size-10 place-items-center rounded-xl bg-indigo-600 text-white shadow-sm">
              <CalendarDays aria-hidden="true" size={21} />
            </span>
            <div>
              <p className="font-semibold tracking-tight">Métrica Eventos</p>
              <p className="text-xs text-slate-500">Panel de administración</p>
            </div>
          </div>
          <DemoRoleSelector />
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-5 py-8 sm:px-8 sm:py-12">
        <CreateEventPage />
      </main>
    </div>
  )
}
