import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';
import { useAuth } from '@/contexts/AuthContext';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ModeToggle } from '@/components/ModeToggle';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormData = z.infer<typeof loginSchema>;

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
const SHOW_TEST_DROPDOWN = import.meta.env.DEV;

const LoginPage = () => {
  const navigate = useNavigate();
  const { login, isLoading } = useAuth();

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: 'admin@example.com',
      password: 'Admin123!',
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

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data.email, data.password);
      toast.success('Login successful!');
      navigate('/dashboard', { replace: true });
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Login failed';
      toast.error(errorMessage);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="absolute top-4 right-4">
        <ModeToggle />
      </div>
      <Card className="w-full max-w-sm shadow-lg">
        <CardHeader className="space-y-1 text-center">
          <CardTitle className="text-2xl font-semibold">Welcome back</CardTitle>
          <p className="text-sm text-muted-foreground">
            Enter your email to sign in to your account
          </p>
        </CardHeader>
        <CardContent className="space-y-6">
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
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="email"
                render={({ field }: { field: any }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="email"
                        placeholder="name@example.com"
                        disabled={isLoading}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="password"
                render={({ field }: { field: any }) => (
                  <FormItem>
                    <FormLabel>Password</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="password"
                        placeholder="••••••••"
                        disabled={isLoading}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button
                type="submit"
                className="w-full"
                disabled={isLoading}
              >
                {isLoading ? 'Signing in...' : 'Sign In'}
              </Button>
            </form>
          </Form>
          
          <div className="text-center text-sm text-muted-foreground">
            Don't have an account?{' '}
            <Link 
              to="/register" 
              className="font-medium text-primary hover:underline"
            >
              Sign up
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default LoginPage;