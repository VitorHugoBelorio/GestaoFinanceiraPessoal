import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { App } from './App'
import { AuthProvider } from './auth/AuthContext'
import { AppMotionProvider } from './motion/MotionProvider'
import { ThemeProvider } from './theme/ThemeContext'
import './style.css'

createRoot(document.getElementById('app')!).render(
  <StrictMode>
    <BrowserRouter>
      <ThemeProvider>
        <AppMotionProvider><AuthProvider><App /></AuthProvider></AppMotionProvider>
      </ThemeProvider>
    </BrowserRouter>
  </StrictMode>,
)
