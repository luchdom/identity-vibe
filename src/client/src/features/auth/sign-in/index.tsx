import { useSearchParams } from 'react-router-dom'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { UserAuthForm } from './components/user-auth-form'

export function SignIn() {
  const [searchParams] = useSearchParams()
  const redirect = searchParams.get('redirect')

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8">
        <Card className='gap-4'>
          <CardHeader>
            <CardTitle className='text-lg tracking-tight'>Sign in</CardTitle>
            <CardDescription>
              Enter your email and password below to <br />
              log into your account
            </CardDescription>
          </CardHeader>
          <CardContent>
            <UserAuthForm redirectTo={redirect || undefined} />
          </CardContent>
          <CardFooter>
            <p className='text-muted-foreground px-8 text-center text-sm'>
              By clicking sign in, you agree to our{' '}
              <a
                href='/terms'
                className='hover:text-primary underline underline-offset-4'
              >
                Terms of Service
              </a>{' '}
              and{' '}
              <a
                href='/privacy'
                className='hover:text-primary underline underline-offset-4'
              >
                Privacy Policy
              </a>
              .
            </p>
          </CardFooter>
        </Card>
      </div>
    </div>
  )
}
