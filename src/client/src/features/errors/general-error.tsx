import { Button } from '@/components/ui/button'

interface GeneralErrorProps {
  error?: Error
}

export function GeneralError({ error }: GeneralErrorProps) {
  return (
    <div className="flex h-screen w-full flex-col items-center justify-center">
      <div className="text-center">
        <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-red-100 p-3">
          <svg className="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.694-.833-2.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" />
          </svg>
        </div>
        <h1 className="mb-2 text-2xl font-bold tracking-tight">Something went wrong!</h1>
        <p className="mb-8 text-muted-foreground">
          {error?.message || 'We apologize for the inconvenience. Please try again.'}
        </p>
        <Button onClick={() => window.location.reload()}>
          Try again
        </Button>
      </div>
    </div>
  )
}