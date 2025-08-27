import { useEffect, useState } from 'react'
import { useLocation } from 'react-router-dom'

export function NavigationProgress() {
  const [isLoading, setIsLoading] = useState(false)
  const location = useLocation()

  useEffect(() => {
    setIsLoading(true)
    const timer = setTimeout(() => setIsLoading(false), 100)
    return () => clearTimeout(timer)
  }, [location])

  if (!isLoading) return null

  return (
    <div className="fixed top-0 left-0 right-0 z-50">
      <div className="h-0.5 bg-primary/30 overflow-hidden">
        <div className="h-full bg-primary animate-pulse" />
      </div>
    </div>
  )
}