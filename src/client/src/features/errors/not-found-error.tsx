import { Button } from '@/components/ui/button'
import { useNavigate } from 'react-router-dom'

export function NotFoundError() {
  const navigate = useNavigate()

  return (
    <div className="flex h-screen w-full flex-col items-center justify-center">
      <div className="text-center">
        <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-orange-100 p-3">
          <svg className="h-6 w-6 text-orange-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
          </svg>
        </div>
        <h1 className="mb-2 text-4xl font-bold tracking-tight">404</h1>
        <p className="mb-8 text-muted-foreground">
          Sorry, we couldn't find the page you're looking for.
        </p>
        <Button onClick={() => navigate('/')}>
          Go back home
        </Button>
      </div>
    </div>
  )
}