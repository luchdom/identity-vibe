'use client';

import { useState } from 'react';
import { z } from 'zod';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import { Loader2, LogIn } from 'lucide-react';
import { toast } from 'sonner';
import { useAuth } from '@/contexts/auth-context';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

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
const SHOW_TEST_DROPDOWN = process.env.NODE_ENV === 'development';

const formSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(1, 'Please enter your password'),
});

type FormValues = z.infer<typeof formSchema>;

export function LoginForm({
  className,
  ...props
}: React.ComponentProps<"div">) {
  const [isLoading, setIsLoading] = useState(false);
  const router = useRouter();
  const { login } = useAuth();

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  // Handle test user selection
  const handleTestUserSelect = (userId: string) => {
    const selectedUser = TEST_USERS.find(user => user.id === userId);
    if (selectedUser) {
      form.setValue('email', selectedUser.email);
      form.setValue('password', selectedUser.password);
      toast.success(`Credentials filled for ${selectedUser.label}`);
    }
  };

  async function onSubmit(data: FormValues) {
    setIsLoading(true);

    try {
      const result = await login(data.email, data.password);
      
      if (result.success) {
        toast.success(`Welcome back, ${data.email}!`);
        router.push('/dashboard');
      } else {
        toast.error(result.error || 'Login failed');
      }
    } catch (error) {
      console.error('Login error:', error);
      toast.error('An unexpected error occurred');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Welcome back</CardTitle>
          <CardDescription>
            Sign in to your admin account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="grid gap-6">
              {SHOW_TEST_DROPDOWN && (
                <div className="space-y-2">
                  <label className="text-sm font-medium">Quick Test Login</label>
                  <Select onValueChange={handleTestUserSelect}>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select test user..." />
                    </SelectTrigger>
                    <SelectContent>
                      {TEST_USERS.map((user) => (
                        <SelectItem key={user.id} value={user.id}>
                          <div className="flex flex-col items-start">
                            <span className="font-medium">{user.label}</span>
                            <span className="text-xs text-muted-foreground">{user.description}</span>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <p className="text-xs text-muted-foreground">
                    Select a test user to auto-fill login credentials
                  </p>
                </div>
              )}
              
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input 
                        type="email"
                        placeholder="admin@example.com"
                        {...field} 
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Password</FormLabel>
                    <FormControl>
                      <Input 
                        type="password"
                        placeholder="********"
                        {...field} 
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              
              <Button type="submit" className="w-full" disabled={isLoading}>
                {isLoading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Signing in...
                  </>
                ) : (
                  <>
                    <LogIn className="mr-2 h-4 w-4" />
                    Sign In
                  </>
                )}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}
