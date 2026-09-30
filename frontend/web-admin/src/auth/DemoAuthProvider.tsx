import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { requestDemoToken } from '../api/eventsApi'

export type DemoRole = 'Admin' | 'User'

interface DemoAuthValue {
  token: string | null
  role: DemoRole | null
  isLoading: boolean
  error: string | null
  selectRole: (role: DemoRole) => Promise<void>
}

const DemoAuthContext = createContext<DemoAuthValue | null>(null)

export function DemoAuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null)
  const [role, setRole] = useState<DemoRole | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function selectRole(nextRole: DemoRole) {
    setIsLoading(true)
    setError(null)
    setToken(null)
    try {
      setToken(await requestDemoToken(nextRole))
      setRole(nextRole)
    } catch {
      setRole(null)
      setError('No pudimos iniciar la sesión de demostración.')
    } finally {
      setIsLoading(false)
    }
  }

  const value = useMemo(
    () => ({ token, role, isLoading, error, selectRole }),
    [token, role, isLoading, error],
  )

  return <DemoAuthContext.Provider value={value}>{children}</DemoAuthContext.Provider>
}

export function useDemoAuth() {
  const context = useContext(DemoAuthContext)
  if (!context) throw new Error('useDemoAuth debe usarse dentro de DemoAuthProvider.')
  return context
}
