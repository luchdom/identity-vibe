import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { initializeTelemetry, setupApiTracing } from './lib/telemetry'

// Initialize OpenTelemetry before rendering the app
initializeTelemetry()
setupApiTracing()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
