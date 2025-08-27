import { createContext, useContext, type ReactNode } from 'react'

interface LayoutContextType {
  layout: 'default'
}

const LayoutContext = createContext<LayoutContextType>({ layout: 'default' })

interface LayoutProviderProps {
  children: ReactNode
}

export function LayoutProvider({ children }: LayoutProviderProps) {
  return (
    <LayoutContext.Provider value={{ layout: 'default' }}>
      {children}
    </LayoutContext.Provider>
  )
}

export function useLayout() {
  return useContext(LayoutContext)
}