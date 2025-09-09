import { useState } from 'react'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { Loader2, LogIn } from 'lucide-react'
import { toast } from 'sonner'
import { useAuth } from '@/contexts/AuthContext'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/password-input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

// Test user types and configuration
interface TestUser {
  id: string;
  label: string;
  email: string;
  password: string;
  description: string;
}

const TEST_USERS: TestUser[] = [
  {
    id: 'admin',
    label: 'Admin User',
    email: 'admin@example.com',
    password: 'Admin123!',
    description: 'Full admin access with all permissions'
  },
  {
    id: 'user',
    label: 'Regular User',
    email: 'user@example.com',
    password: 'User123!',
    description: 'Standard user access with limited permissions'
  }
];

// Show dropdown only in development environment
const SHOW_TEST_DROPDOWN = import.meta.env.DEV || import.meta.env.VITE_APP_ENV === 'development';

// Debug environment detection
console.log('Environment Detection:', {
  DEV: import.meta.env.DEV,
  VITE_APP_ENV: import.meta.env.VITE_APP_ENV,
  SHOW_TEST_DROPDOWN
});

const formSchema = z.object({
  email: z.email({
    error: (iss) => (iss.input === '' ? 'Please enter your email' : undefined),
  }),
  password: z
    .string()
    .min(1, 'Please enter your password')
    .min(7, 'Password must be at least 7 characters long'),
})

interface UserAuthFormProps extends React.HTMLAttributes<HTMLFormElement> {
  redirectTo?: string
}

export function UserAuthForm({
  className,
  redirectTo,
  ...props
}: UserAuthFormProps) {
  const [isLoading, setIsLoading] = useState(false)
  const navigate = useNavigate()
  const { login } = useAuth()

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  // Handle test user selection
  const handleTestUserSelect = (userId: string) => {
    const selectedUser = TEST_USERS.find(user => user.id === userId);
    if (selectedUser) {
      form.setValue('email', selectedUser.email);
      form.setValue('password', selectedUser.password);
      toast.success(`Credentials filled for ${selectedUser.label}`);
    }
  };

  async function onSubmit(data: z.infer<typeof formSchema>) {
    setIsLoading(true)

    try {
      const result = await login(data.email, data.password)
      
      if (result.success) {
        toast.success(`Welcome back, ${data.email}!`)
        
        // Redirect to the stored location or default to dashboard
        const targetPath = redirectTo || '/dashboard'
        navigate(targetPath, { replace: true })
      } else {
        toast.error(result.error || 'Login failed')
      }
    } catch (error) {
      console.error('Login error:', error)
      toast.error('An unexpected error occurred')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className={cn('grid gap-3', className)}
        {...props}
      >
        {SHOW_TEST_DROPDOWN && (
          <div className='space-y-2'>
            <label className='text-sm font-medium'>Quick Test Login</label>
            <Select onValueChange={handleTestUserSelect}>
              <SelectTrigger className='w-full'>
                <SelectValue placeholder='Select test user...' />
              </SelectTrigger>
              <SelectContent>
                {TEST_USERS.map((user) => (
                  <SelectItem key={user.id} value={user.id}>
                    <div className='flex flex-col items-start'>
                      <span className='font-medium'>{user.label}</span>
                      <span className='text-xs text-muted-foreground'>{user.description}</span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className='text-xs text-muted-foreground'>
              Select a test user to auto-fill login credentials
            </p>
          </div>
        )}
        <FormField
          control={form.control}
          name='email'
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input placeholder='name@example.com' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name='password'
          render={({ field }) => (
            <FormItem className='relative'>
              <FormLabel>Password</FormLabel>
              <FormControl>
                <PasswordInput placeholder='********' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button className='mt-2' disabled={isLoading}>
          {isLoading ? <Loader2 className='animate-spin' /> : <LogIn />}
          Sign in
        </Button>
      </form>
    </Form>
  )
}
