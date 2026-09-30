import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import { DemoAuthProvider } from './auth/DemoAuthProvider'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <DemoAuthProvider>
      <App />
    </DemoAuthProvider>
  </StrictMode>,
)
