import { ShieldCheck } from 'lucide-react'
import { useDemoAuth, type DemoRole } from './DemoAuthProvider'

export function DemoRoleSelector() {
  const { role, isLoading, error, selectRole } = useDemoAuth()

  return (
    <div className="flex flex-col items-end gap-1">
      <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
        <ShieldCheck aria-hidden="true" size={17} className="text-indigo-600" />
        <span className="hidden sm:inline">Rol de demo</span>
        <select
          aria-label="Rol de demo"
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
          value={role ?? ''}
          disabled={isLoading}
          onChange={(event) => event.target.value && void selectRole(event.target.value as DemoRole)}
        >
          <option value="">Seleccionar</option>
          <option value="Admin">Admin</option>
          <option value="User">User</option>
        </select>
      </label>
      {isLoading && <span className="text-xs text-slate-500">Iniciando sesión…</span>}
      {error && <span role="alert" className="text-xs text-red-700">{error}</span>}
    </div>
  )
}
