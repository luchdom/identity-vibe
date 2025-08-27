# Frontend Development Guide

This document covers React frontend patterns, components, and best practices.

## Technology Stack

- **React 18**: Modern React with functional components and hooks
- **TypeScript**: Full type safety throughout the application
- **Vite**: Fast build tool and development server
- **Tailwind CSS**: Utility-first CSS framework
- **shadcn/ui**: Component library built on Radix UI and Tailwind CSS
- **React Hook Form**: Performant forms with easy validation
- **Zod**: TypeScript-first schema validation
- **Axios**: Promise-based HTTP client with automatic token management
- **React Router**: Declarative routing for React
- **Playwright**: End-to-end testing framework

## Architecture Patterns

### Component Organization (Atomic Design)
```
src/
├── components/           # Reusable components
│   ├── ui/              # shadcn/ui components (atoms)
│   └── [Component].tsx  # Custom components (molecules)
├── contexts/            # React contexts (Auth, Theme, Search)
├── hooks/               # Custom React hooks
├── lib/                 # Utility functions and configurations
├── pages/               # Page components (organisms)
├── features/            # Feature-based components
└── tests/               # Test files (Playwright, Jest)
```

### State Management Patterns
- **Context API**: Global state (AuthContext, ThemeContext, SearchProvider)
- **Local State**: useState for component-specific state
- **Form State**: React Hook Form with Zod validation
- **Server State**: Axios with interceptors for API communication

## shadcn/ui Integration

### Installation Pattern
```bash
# Add individual components as needed
npx shadcn@latest add button card input form dropdown-menu

# Components are added to src/components/ui/
# Import: import { Button } from '@/components/ui/button'
```

### Theme System
- **CSS Variables**: Light/dark mode with CSS variables
- **Class-based Switching**: Automatic theme class application
- **System Preference**: Detects user's system theme preference

### Component Usage
```typescript
// ✅ Using shadcn/ui components with proper TypeScript
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function Dashboard() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Dashboard</CardTitle>
      </CardHeader>
      <CardContent>
        <Button variant="default" size="lg">
          Download Report
        </Button>
      </CardContent>
    </Card>
  )
}
```

## Authentication Flow

### Protected Routes
```typescript
// ✅ Protected route pattern
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <LoadingSpinner />
  }

  if (!isAuthenticated) {
    return <Navigate to="/sign-in" replace />
  }

  return (
    <SearchProvider>
      <LayoutProvider>
        <AuthenticatedLayout>{children}</AuthenticatedLayout>
      </LayoutProvider>
    </SearchProvider>
  )
}
```

### Token Management
```typescript
// ✅ Automatic token refresh via Axios interceptors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) {
        const response = await axios.post(`${BASE_URL}/auth/refresh`, { refreshToken })
        const { accessToken } = response.data
        
        localStorage.setItem('accessToken', accessToken)
        originalRequest.headers.Authorization = `Bearer ${accessToken}`
        return api(originalRequest)
      }
    }
    return Promise.reject(error)
  }
)
```

## Styling Approach

### Tailwind CSS Patterns
```typescript
// ✅ Responsive, mobile-first design
<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
  <Card className="p-6">
    <div className="flex items-center justify-between">
      <h3 className="text-sm font-medium text-muted-foreground">Total Revenue</h3>
      <DollarSign className="h-4 w-4 text-muted-foreground" />
    </div>
    <div className="mt-2">
      <p className="text-2xl font-bold">$45,231.89</p>
      <p className="text-xs text-muted-foreground">+20.1% from last month</p>
    </div>
  </Card>
</div>
```

### Component Variants with CVA
```typescript
// ✅ Using class-variance-authority for component variations
const buttonVariants = cva(
  "inline-flex items-center justify-center rounded-md font-medium transition-colors",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-primary/90",
        destructive: "bg-destructive text-destructive-foreground hover:bg-destructive/90",
        outline: "border border-input hover:bg-accent hover:text-accent-foreground"
      },
      size: {
        default: "h-10 px-4 py-2",
        sm: "h-9 rounded-md px-3",
        lg: "h-11 rounded-md px-8"
      }
    }
  }
)
```

## Form Handling

### React Hook Form + Zod Pattern
```typescript
// ✅ Type-safe forms with validation
const formSchema = z.object({
  email: z.string().email("Invalid email address"),
  password: z.string().min(8, "Password must be at least 8 characters")
})

type FormValues = z.infer<typeof formSchema>

export function LoginForm() {
  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { email: "", password: "" }
  })

  const onSubmit = async (values: FormValues) => {
    try {
      await api.post('/auth/login', values)
      navigate('/dashboard')
    } catch (error) {
      form.setError("root", { message: "Invalid credentials" })
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input placeholder="Enter your email" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit">Sign In</Button>
      </form>
    </Form>
  )
}
```

## Testing Strategy

### Playwright E2E Testing
```typescript
// ✅ End-to-end test patterns
test('user can sign in and access dashboard', async ({ page }) => {
  await page.goto('/sign-in')
  
  await page.fill('[name="email"]', 'admin@example.com')
  await page.fill('[name="password"]', 'Admin123!')
  await page.click('button[type="submit"]')
  
  await expect(page).toHaveURL('/dashboard')
  await expect(page.locator('h1')).toContainText('Dashboard')
})
```

### Component Testing
```typescript
// ✅ Component testing with React Testing Library
test('LoginForm displays validation errors', async () => {
  render(<LoginForm />)
  
  fireEvent.click(screen.getByRole('button', { name: 'Sign In' }))
  
  await waitFor(() => {
    expect(screen.getByText('Invalid email address')).toBeInTheDocument()
    expect(screen.getByText('Password must be at least 8 characters')).toBeInTheDocument()
  })
})
```

## Performance Optimization

### Code Splitting
```typescript
// ✅ Route-based code splitting
const Dashboard = lazy(() => import('@/features/dashboard'))
const Settings = lazy(() => import('@/features/settings'))

function App() {
  return (
    <Suspense fallback={<LoadingSpinner />}>
      <Routes>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/settings" element={<Settings />} />
      </Routes>
    </Suspense>
  )
}
```

### Memoization
```typescript
// ✅ Memoizing expensive calculations
const expensiveValue = useMemo(() => {
  return calculateExpensiveValue(data)
}, [data])

// ✅ Memoizing components
const MemoizedCard = memo(({ title, content }) => (
  <Card>
    <CardHeader>
      <CardTitle>{title}</CardTitle>
    </CardHeader>
    <CardContent>{content}</CardContent>
  </Card>
))
```

## Error Handling

### Error Boundaries
```typescript
// ✅ React error boundary for component errors
class ErrorBoundary extends Component {
  constructor(props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error) {
    return { hasError: true }
  }

  render() {
    if (this.state.hasError) {
      return <ErrorFallback />
    }
    return this.props.children
  }
}
```

### API Error Handling
```typescript
// ✅ Centralized error handling via Axios interceptors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const silentErrors = ['LOGIN_ERROR', 'REGISTRATION_ERROR']
    if (!originalRequest.skipErrorToast && !silentErrors.includes(originalRequest.errorContext)) {
      toast.error(error.response?.data?.message || 'An error occurred')
    }
    return Promise.reject(error)
  }
)
```

## Accessibility (a11y)

### Best Practices
- **Semantic HTML**: Proper HTML structure and ARIA labels
- **Keyboard Navigation**: Full keyboard accessibility support
- **Screen Reader**: Compatible with assistive technologies
- **Color Contrast**: WCAG-compliant color schemes
- **Focus Management**: Proper focus handling in modals and forms

```typescript
// ✅ Accessible component example
<Button
  aria-label="Close dialog"
  onClick={handleClose}
  className="sr-only focus:not-sr-only"
>
  <X className="h-4 w-4" />
  <span className="sr-only">Close</span>
</Button>
```

## Environment Configuration

### Development vs Production
- **Development**: Hot module reloading, debug tools, detailed errors
- **Production**: Optimized builds, error tracking, performance monitoring
- **Environment Variables**: Use `.env` files for configuration

```typescript
// ✅ Environment-based configuration
const config = {
  apiUrl: import.meta.env.VITE_API_URL || 'http://localhost:5002',
  enableDevTools: import.meta.env.DEV,
  version: import.meta.env.VITE_APP_VERSION
}
```