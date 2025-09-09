# Frontend Development Guide - Next.js

<document_overview>
This document covers Next.js App Router patterns, components, and best practices with kebab-case naming conventions.
</document_overview>

## Technology Stack

<nextjs_technology_stack>
  <framework name="Next.js" version="14+">App Router with React Server Components</framework>
  <framework name="React" version="18+">Modern React with Server Components and Client Components</framework>
  <framework name="TypeScript" version="5+">Full type safety with strict mode</framework>
  <styling name="Tailwind CSS" version="3+">Utility-first CSS framework</styling>
  <components name="shadcn/ui">Radix UI primitives with Tailwind styling</components>
  <forms name="React Hook Form">Performant forms with built-in validation</forms>
  <validation name="Zod">TypeScript-first schema validation</validation>
  <http name="Axios">HTTP client with interceptors for automatic token management</http>
  <auth name="NextAuth.js">Authentication for Next.js applications</auth>
  <testing name="Playwright">End-to-end testing framework</testing>
  <testing name="Jest">Unit testing with React Testing Library</testing>
</nextjs_technology_stack>

## Project Structure

<nextjs_project_structure>
<code_example language="text">
src/
├── app/                          # App Router pages and layouts
│   ├── (auth)/                   # Route group for auth pages
│   │   ├── login/
│   │   │   └── page.tsx
│   │   ├── register/
│   │   │   └── page.tsx
│   │   └── layout.tsx
│   ├── (dashboard)/              # Protected route group
│   │   ├── dashboard/
│   │   │   └── page.tsx
│   │   ├── user-profile/
│   │   │   └── page.tsx
│   │   └── layout.tsx
│   ├── api/                      # API routes
│   │   ├── auth/
│   │   │   └── [...nextauth]/
│   │   │       └── route.ts
│   │   └── users/
│   │       └── route.ts
│   ├── layout.tsx                # Root layout
│   ├── page.tsx                  # Home page
│   └── global.css                # Global styles
├── components/                    # Shared components
│   ├── ui/                       # shadcn/ui components
│   │   ├── button.tsx
│   │   ├── card.tsx
│   │   └── dialog.tsx
│   ├── layout/                   # Layout components
│   │   ├── header-nav.tsx
│   │   ├── sidebar-menu.tsx
│   │   └── footer-info.tsx
│   └── features/                 # Feature-specific components
│       ├── user-card.tsx
│       └── order-list.tsx
├── lib/                          # Utilities and configurations
│   ├── api-client.ts
│   ├── auth-config.ts
│   └── utils.ts
├── hooks/                        # Custom React hooks
│   ├── use-auth.ts
│   └── use-debounce.ts
├── types/                        # TypeScript type definitions
│   ├── api-types.ts
│   └── user-types.ts
├── config/                       # Configuration files
│   ├── site-config.ts
│   └── api-endpoints.ts
└── middleware.ts                 # Next.js middleware
</code_example>
</nextjs_project_structure>

## Naming Conventions

<nextjs_naming_conventions>
  <mandatory_rule priority="critical">
    Next.js projects MUST use kebab-case for files and folders to ensure clean, SEO-friendly URLs in App Router.
  </mandatory_rule>

  <file_and_folder_naming>
    <rule>Use kebab-case (lowercase with hyphens) for all files and folders</rule>
    <good_example>
<code_example language="text">
✅ CORRECT - Kebab-case files
user-profile.tsx
api-utils.ts
auth-context.tsx
order-management.tsx
components/header-nav.tsx
app/user-settings/page.tsx
</code_example>
    </good_example>
    <bad_example>
<code_example language="text">
❌ WRONG - Other naming styles
UserProfile.tsx        // PascalCase
user_profile.tsx       // snake_case
userProfile.tsx        // camelCase
USERPROFILE.tsx        // UPPERCASE
</code_example>
    </bad_example>
  </file_and_folder_naming>

  <react_components>
    <rule>Use PascalCase for React component names</rule>
    <code_example language="typescript">
// File: components/user-profile.tsx
export function UserProfile() {
  return <div>...</div>
}

// File: components/header-nav.tsx
export function HeaderNav() {
  return <nav>...</nav>
}

// File: app/dashboard/page.tsx
export default function DashboardPage() {
  return <main>...</main>
}
</code_example>
  </react_components>

  <variables_functions_props>
    <rule>Use camelCase for variables, functions, and props</rule>
    <code_example language="typescript">
// Variables
const userName = "John Doe"
const isLoggedIn = true
const orderCount = 5

// Functions
const handleSubmit = () => {}
const fetchUserData = async () => {}
const validateEmail = (email: string) => {}

// Props
interface UserCardProps {
  firstName: string
  lastName: string
  isActive: boolean
  onUpdate: () => void
}
</code_example>
  </variables_functions_props>

  <constants>
    <rule>Use SCREAMING_SNAKE_CASE for constants</rule>
    <code_example language="typescript">
// File: config/api-endpoints.ts
export const API_BASE_URL = "http://localhost:5002"
export const MAX_RETRY_ATTEMPTS = 3
export const SESSION_TIMEOUT_MS = 1800000
export const DEFAULT_PAGE_SIZE = 20
</code_example>
  </constants>

  <css_classes_and_ids>
    <rule>Use kebab-case for CSS classes and IDs</rule>
    <code_example language="typescript">
// Tailwind classes and custom CSS
<div className="user-card-container">
  <div id="main-header">
    <button className="submit-button primary-action">
      Submit
    </button>
  </div>
</div>
</code_example>
  </css_classes_and_ids>

  <api_routes>
    <rule>Use kebab-case for API routes to match REST conventions</rule>
    <code_example language="text">
/api/users
/api/user-profile
/api/auth/login
/api/order-management
/api/product-categories
</code_example>
  </api_routes>

  <rationale>
    <point>Kebab-case files create clean, SEO-friendly URLs in Next.js App Router (e.g., /user-profile)</point>
    <point>Consistent with web standards where URLs use hyphens</point>
    <point>Prevents URL encoding issues with spaces or special characters</point>
    <point>Maintains consistency across file system and browser URLs</point>
    <point>Easy to read and type without holding Shift key</point>
  </rationale>
</nextjs_naming_conventions>

## App Router Architecture

<nextjs_app_router>
  <server_components>
    <rule>Default to Server Components for better performance</rule>
    <code_example language="typescript">
// File: app/products/page.tsx
// Server Component (default) - runs on server
import { getProducts } from '@/lib/api/product-service'

export default async function ProductsPage() {
  const products = await getProducts() // Direct database/API call
  
  return (
    <div className="grid gap-4">
      {products.map(product => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  )
}
</code_example>
  </server_components>

  <client_components>
    <rule>Use Client Components only when needed for interactivity</rule>
    <code_example language="typescript">
// File: components/search-bar.tsx
'use client' // Required for client-side interactivity

import { useState } from 'react'

export function SearchBar() {
  const [query, setQuery] = useState('')
  
  return (
    <input
      type="search"
      value={query}
      onChange={(e) => setQuery(e.target.value)}
      placeholder="Search..."
    />
  )
}
</code_example>
  </client_components>

  <layouts>
    <rule>Use layouts for shared UI across routes</rule>
    <code_example language="typescript">
// File: app/(dashboard)/layout.tsx
import { SidebarMenu } from '@/components/layout/sidebar-menu'
import { HeaderNav } from '@/components/layout/header-nav'

export default function DashboardLayout({
  children
}: {
  children: React.ReactNode
}) {
  return (
    <div className="flex h-screen">
      <SidebarMenu />
      <div className="flex-1 flex flex-col">
        <HeaderNav />
        <main className="flex-1 p-6">
          {children}
        </main>
      </div>
    </div>
  )
}
</code_example>
  </layouts>

  <route_groups>
    <rule>Use route groups to organize routes without affecting URLs</rule>
    <code_example language="text">
app/
├── (auth)/           # No URL impact
│   ├── login/       # URL: /login
│   └── register/    # URL: /register
├── (marketing)/     # No URL impact
│   ├── about/       # URL: /about
│   └── pricing/     # URL: /pricing
└── (dashboard)/     # No URL impact
    ├── settings/    # URL: /settings
    └── profile/     # URL: /profile
</code_example>
  </route_groups>

  <parallel_routes>
    <rule>Use parallel routes for simultaneous rendering</rule>
    <code_example language="typescript">
// File: app/dashboard/@metrics/page.tsx
export default function MetricsSlot() {
  return <MetricsPanel />
}

// File: app/dashboard/@activity/page.tsx
export default function ActivitySlot() {
  return <ActivityFeed />
}

// File: app/dashboard/layout.tsx
export default function Layout({
  children,
  metrics,
  activity
}: {
  children: React.ReactNode
  metrics: React.ReactNode
  activity: React.ReactNode
}) {
  return (
    <div className="grid grid-cols-3 gap-4">
      <div className="col-span-2">{children}</div>
      <div>
        {metrics}
        {activity}
      </div>
    </div>
  )
}
</code_example>
  </parallel_routes>

  <intercepting_routes>
    <rule>Use intercepting routes for modals and overlays</rule>
    <code_example language="text">
app/
├── products/
│   └── [id]/
│       └── page.tsx
└── @modal/
    └── (.)products/
        └── [id]/
            └── page.tsx  # Intercepts /products/[id] for modal
</code_example>
  </intercepting_routes>
</nextjs_app_router>

## Component Patterns

<nextjs_component_patterns>
  <shadcn_integration>
    <rule>Use shadcn/ui components with customization</rule>
    <code_example language="typescript">
// File: components/ui/button.tsx
import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none ring-offset-background",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-primary/90",
        destructive: "bg-destructive text-destructive-foreground hover:bg-destructive/90",
        outline: "border border-input hover:bg-accent hover:text-accent-foreground",
        secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80",
        ghost: "hover:bg-accent hover:text-accent-foreground",
        link: "underline-offset-4 hover:underline text-primary",
      },
      size: {
        default: "h-10 py-2 px-4",
        sm: "h-9 px-3 rounded-md",
        lg: "h-11 px-8 rounded-md",
        icon: "h-10 w-10",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }
</code_example>
  </shadcn_integration>

  <compound_components>
    <rule>Create compound components for complex UI</rule>
    <code_example language="typescript">
// File: components/data-table.tsx
'use client'

import { createContext, useContext } from 'react'

const DataTableContext = createContext<any>(null)

export function DataTable({ children, data }: any) {
  return (
    <DataTableContext.Provider value={{ data }}>
      <div className="w-full overflow-auto">
        <table className="w-full caption-bottom text-sm">
          {children}
        </table>
      </div>
    </DataTableContext.Provider>
  )
}

DataTable.Header = function DataTableHeader({ children }: any) {
  return (
    <thead className="[&_tr]:border-b">
      {children}
    </thead>
  )
}

DataTable.Body = function DataTableBody({ children }: any) {
  const { data } = useContext(DataTableContext)
  return <tbody className="[&_tr:last-child]:border-0">{children}</tbody>
}

DataTable.Row = function DataTableRow({ children }: any) {
  return (
    <tr className="border-b transition-colors hover:bg-muted/50">
      {children}
    </tr>
  )
}

// Usage
export function OrderTable({ orders }: { orders: Order[] }) {
  return (
    <DataTable data={orders}>
      <DataTable.Header>
        <DataTable.Row>
          <th>Order ID</th>
          <th>Customer</th>
          <th>Total</th>
        </DataTable.Row>
      </DataTable.Header>
      <DataTable.Body>
        {orders.map(order => (
          <DataTable.Row key={order.id}>
            <td>{order.id}</td>
            <td>{order.customer}</td>
            <td>{order.total}</td>
          </DataTable.Row>
        ))}
      </DataTable.Body>
    </DataTable>
  )
}
</code_example>
  </compound_components>

  <render_props_pattern>
    <rule>Use render props for flexible component composition</rule>
    <code_example language="typescript">
// File: components/data-fetcher.tsx
'use client'

interface DataFetcherProps<T> {
  url: string
  children: (data: T | null, loading: boolean, error: Error | null) => React.ReactNode
}

export function DataFetcher<T>({ url, children }: DataFetcherProps<T>) {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<Error | null>(null)

  useEffect(() => {
    fetch(url)
      .then(res => res.json())
      .then(setData)
      .catch(setError)
      .finally(() => setLoading(false))
  }, [url])

  return <>{children(data, loading, error)}</>
}

// Usage
<DataFetcher<User[]> url="/api/users">
  {(users, loading, error) => {
    if (loading) return <Spinner />
    if (error) return <ErrorMessage error={error} />
    return <UserList users={users!} />
  }}
</DataFetcher>
</code_example>
  </render_props_pattern>
</nextjs_component_patterns>

## State Management

<nextjs_state_management>
  <context_pattern>
    <rule>Use React Context for global state with Server Component support</rule>
    <code_example language="typescript">
// File: contexts/auth-context.tsx
'use client'

import { createContext, useContext, useState, useEffect } from 'react'
import { User } from '@/types/user-types'
import { getSession } from '@/lib/auth-service'

interface AuthContextType {
  user: User | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    getSession()
      .then(setUser)
      .finally(() => setIsLoading(false))
  }, [])

  const login = async (email: string, password: string) => {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    })
    
    if (response.ok) {
      const user = await response.json()
      setUser(user)
    } else {
      throw new Error('Login failed')
    }
  }

  const logout = async () => {
    await fetch('/api/auth/logout', { method: 'POST' })
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
</code_example>
  </context_pattern>

  <server_state_management>
    <rule>Use Server Components for server-side state</rule>
    <code_example language="typescript">
// File: app/products/page.tsx
import { cookies } from 'next/headers'
import { getProducts } from '@/lib/api/product-service'

export default async function ProductsPage({
  searchParams
}: {
  searchParams: { category?: string; sort?: string }
}) {
  // Server-side state from URL params
  const { category, sort } = searchParams
  
  // Server-side state from cookies
  const preferredView = cookies().get('view')?.value || 'grid'
  
  // Fetch data on server
  const products = await getProducts({ category, sort })
  
  return (
    <div className={preferredView === 'grid' ? 'grid' : 'list'}>
      {products.map(product => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  )
}
</code_example>
  </server_state_management>

  <zustand_integration>
    <rule>Use Zustand for complex client state</rule>
    <code_example language="typescript">
// File: stores/cart-store.ts
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface CartItem {
  id: string
  quantity: number
  price: number
}

interface CartStore {
  items: CartItem[]
  addItem: (item: CartItem) => void
  removeItem: (id: string) => void
  clearCart: () => void
  total: () => number
}

export const useCartStore = create<CartStore>()(
  persist(
    (set, get) => ({
      items: [],
      addItem: (item) => set((state) => {
        const existing = state.items.find(i => i.id === item.id)
        if (existing) {
          return {
            items: state.items.map(i =>
              i.id === item.id
                ? { ...i, quantity: i.quantity + item.quantity }
                : i
            )
          }
        }
        return { items: [...state.items, item] }
      }),
      removeItem: (id) => set((state) => ({
        items: state.items.filter(i => i.id !== id)
      })),
      clearCart: () => set({ items: [] }),
      total: () => {
        const { items } = get()
        return items.reduce((sum, item) => sum + item.price * item.quantity, 0)
      }
    }),
    {
      name: 'cart-storage'
    }
  )
)
</code_example>
  </zustand_integration>
</nextjs_state_management>

## Data Fetching Patterns

<nextjs_data_fetching>
  <server_components_fetching>
    <rule>Fetch data directly in Server Components</rule>
    <code_example language="typescript">
// File: app/users/page.tsx
import { API_BASE_URL } from '@/config/api-endpoints'

async function getUsers() {
  const res = await fetch(`${API_BASE_URL}/users`, {
    next: { revalidate: 60 } // Cache for 60 seconds
  })
  
  if (!res.ok) {
    throw new Error('Failed to fetch users')
  }
  
  return res.json()
}

export default async function UsersPage() {
  const users = await getUsers()
  
  return (
    <div className="grid gap-4">
      {users.map((user: User) => (
        <UserCard key={user.id} user={user} />
      ))}
    </div>
  )
}
</code_example>
  </server_components_fetching>

  <client_components_fetching>
    <rule>Use SWR or React Query for client-side fetching</rule>
    <code_example language="typescript">
// File: components/user-list.tsx
'use client'

import useSWR from 'swr'
import { fetcher } from '@/lib/api-client'

export function UserList() {
  const { data: users, error, isLoading } = useSWR('/api/users', fetcher, {
    refreshInterval: 30000, // Refresh every 30 seconds
    revalidateOnFocus: true
  })

  if (isLoading) return <div>Loading...</div>
  if (error) return <div>Error loading users</div>
  
  return (
    <div className="space-y-4">
      {users?.map((user: User) => (
        <UserCard key={user.id} user={user} />
      ))}
    </div>
  )
}
</code_example>
  </client_components_fetching>

  <server_actions>
    <rule>Use Server Actions for form submissions and mutations</rule>
    <code_example language="typescript">
// File: app/actions/user-actions.ts
'use server'

import { revalidatePath } from 'next/cache'
import { redirect } from 'next/navigation'
import { z } from 'zod'

const CreateUserSchema = z.object({
  name: z.string().min(2),
  email: z.string().email(),
  role: z.enum(['admin', 'user'])
})

export async function createUser(formData: FormData) {
  const validatedFields = CreateUserSchema.safeParse({
    name: formData.get('name'),
    email: formData.get('email'),
    role: formData.get('role')
  })

  if (!validatedFields.success) {
    return {
      errors: validatedFields.error.flatten().fieldErrors
    }
  }

  // Create user in database
  const user = await db.user.create({
    data: validatedFields.data
  })

  revalidatePath('/users')
  redirect(`/users/${user.id}`)
}

// File: components/create-user-form.tsx
import { createUser } from '@/app/actions/user-actions'

export function CreateUserForm() {
  return (
    <form action={createUser}>
      <input name="name" placeholder="Name" required />
      <input name="email" type="email" placeholder="Email" required />
      <select name="role">
        <option value="user">User</option>
        <option value="admin">Admin</option>
      </select>
      <button type="submit">Create User</button>
    </form>
  )
}
</code_example>
  </server_actions>

  <streaming_and_suspense>
    <rule>Use Suspense for streaming and loading states</rule>
    <code_example language="typescript">
// File: app/dashboard/page.tsx
import { Suspense } from 'react'
import { MetricsPanel } from '@/components/metrics-panel'
import { ActivityFeed } from '@/components/activity-feed'
import { MetricsSkeleton } from '@/components/skeletons/metrics-skeleton'
import { ActivitySkeleton } from '@/components/skeletons/activity-skeleton'

export default function DashboardPage() {
  return (
    <div className="grid grid-cols-2 gap-6">
      <Suspense fallback={<MetricsSkeleton />}>
        <MetricsPanel />
      </Suspense>
      
      <Suspense fallback={<ActivitySkeleton />}>
        <ActivityFeed />
      </Suspense>
    </div>
  )
}

// File: components/metrics-panel.tsx
async function getMetrics() {
  const res = await fetch(`${API_BASE_URL}/metrics`)
  return res.json()
}

export async function MetricsPanel() {
  const metrics = await getMetrics()
  
  return (
    <Card>
      <CardHeader>
        <CardTitle>Metrics</CardTitle>
      </CardHeader>
      <CardContent>
        {/* Render metrics */}
      </CardContent>
    </Card>
  )
}
</code_example>
  </streaming_and_suspense>
</nextjs_data_fetching>

## Authentication Patterns

<nextjs_authentication_patterns>
  <nextauth_setup>
    <rule>Configure NextAuth.js for authentication</rule>
    <code_example language="typescript">
// File: app/api/auth/[...nextauth]/route.ts
import NextAuth from 'next-auth'
import CredentialsProvider from 'next-auth/providers/credentials'
import { API_BASE_URL } from '@/config/api-endpoints'

const handler = NextAuth({
  providers: [
    CredentialsProvider({
      name: 'credentials',
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" }
      },
      async authorize(credentials) {
        const res = await fetch(`${API_BASE_URL}/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            email: credentials?.email,
            password: credentials?.password
          })
        })

        const user = await res.json()

        if (res.ok && user) {
          return user
        }
        return null
      }
    })
  ],
  session: {
    strategy: 'jwt'
  },
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        token.accessToken = user.accessToken
        token.refreshToken = user.refreshToken
        token.userId = user.id
      }
      return token
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken
      session.userId = token.userId
      return session
    }
  },
  pages: {
    signIn: '/login',
    error: '/auth-error'
  }
})

export { handler as GET, handler as POST }
</code_example>
  </nextauth_setup>

  <middleware_protection>
    <rule>Use middleware for route protection</rule>
    <code_example language="typescript">
// File: middleware.ts
import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'
import { getToken } from 'next-auth/jwt'

export async function middleware(request: NextRequest) {
  const token = await getToken({ req: request })
  
  const isAuthPage = request.nextUrl.pathname.startsWith('/login') ||
                     request.nextUrl.pathname.startsWith('/register')
  
  if (isAuthPage) {
    if (token) {
      return NextResponse.redirect(new URL('/dashboard', request.url))
    }
  } else {
    if (!token) {
      const from = request.nextUrl.pathname
      return NextResponse.redirect(
        new URL(`/login?from=${encodeURIComponent(from)}`, request.url)
      )
    }
  }
  
  return NextResponse.next()
}

export const config = {
  matcher: [
    '/dashboard/:path*',
    '/admin/:path*',
    '/user-profile/:path*',
    '/login',
    '/register'
  ]
}
</code_example>
  </middleware_protection>

  <session_management>
    <rule>Manage sessions with Server Components</rule>
    <code_example language="typescript">
// File: lib/auth-helpers.ts
import { getServerSession } from 'next-auth'
import { authOptions } from '@/app/api/auth/[...nextauth]/route'
import { redirect } from 'next/navigation'

export async function requireAuth() {
  const session = await getServerSession(authOptions)
  
  if (!session) {
    redirect('/login')
  }
  
  return session
}

// File: app/dashboard/page.tsx
import { requireAuth } from '@/lib/auth-helpers'

export default async function DashboardPage() {
  const session = await requireAuth()
  
  return (
    <div>
      <h1>Welcome, {session.user.name}</h1>
      {/* Dashboard content */}
    </div>
  )
}
</code_example>
  </session_management>
</nextjs_authentication_patterns>

## Form Handling

<nextjs_form_patterns>
  <react_hook_form_with_zod>
    <rule>Use React Hook Form with Zod validation</rule>
    <code_example language="typescript">
// File: components/forms/user-form.tsx
'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { toast } from '@/components/ui/use-toast'

const userFormSchema = z.object({
  email: z.string().email('Invalid email address'),
  firstName: z.string().min(2, 'First name must be at least 2 characters'),
  lastName: z.string().min(2, 'Last name must be at least 2 characters'),
  age: z.coerce.number().min(18, 'Must be at least 18 years old'),
  bio: z.string().max(500, 'Bio must be less than 500 characters').optional()
})

type UserFormValues = z.infer<typeof userFormSchema>

export function UserForm() {
  const form = useForm<UserFormValues>({
    resolver: zodResolver(userFormSchema),
    defaultValues: {
      email: '',
      firstName: '',
      lastName: '',
      age: 18,
      bio: ''
    }
  })

  async function onSubmit(data: UserFormValues) {
    try {
      const response = await fetch('/api/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })

      if (response.ok) {
        toast({
          title: 'Success',
          description: 'User created successfully'
        })
        form.reset()
      } else {
        throw new Error('Failed to create user')
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Something went wrong',
        variant: 'destructive'
      })
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input placeholder="john@example.com" {...field} />
              </FormControl>
              <FormDescription>
                We'll never share your email with anyone else.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="firstName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>First Name</FormLabel>
                <FormControl>
                  <Input placeholder="John" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="lastName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Last Name</FormLabel>
                <FormControl>
                  <Input placeholder="Doe" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="age"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Age</FormLabel>
              <FormControl>
                <Input type="number" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" disabled={form.formState.isSubmitting}>
          {form.formState.isSubmitting ? 'Creating...' : 'Create User'}
        </Button>
      </form>
    </Form>
  )
}
</code_example>
  </react_hook_form_with_zod>

  <server_action_forms>
    <rule>Use Server Actions with progressive enhancement</rule>
    <code_example language="typescript">
// File: components/forms/contact-form.tsx
import { submitContactForm } from '@/app/actions/contact-actions'
import { SubmitButton } from '@/components/submit-button'

export function ContactForm() {
  return (
    <form action={submitContactForm} className="space-y-4">
      <div>
        <label htmlFor="name" className="block text-sm font-medium">
          Name
        </label>
        <input
          id="name"
          name="name"
          type="text"
          required
          className="mt-1 block w-full rounded-md border-gray-300"
        />
      </div>

      <div>
        <label htmlFor="email" className="block text-sm font-medium">
          Email
        </label>
        <input
          id="email"
          name="email"
          type="email"
          required
          className="mt-1 block w-full rounded-md border-gray-300"
        />
      </div>

      <div>
        <label htmlFor="message" className="block text-sm font-medium">
          Message
        </label>
        <textarea
          id="message"
          name="message"
          rows={4}
          required
          className="mt-1 block w-full rounded-md border-gray-300"
        />
      </div>

      <SubmitButton />
    </form>
  )
}

// File: components/submit-button.tsx
'use client'

import { useFormStatus } from 'react-dom'

export function SubmitButton() {
  const { pending } = useFormStatus()

  return (
    <button
      type="submit"
      disabled={pending}
      className="px-4 py-2 bg-blue-600 text-white rounded-md disabled:opacity-50"
    >
      {pending ? 'Sending...' : 'Send Message'}
    </button>
  )
}
</code_example>
  </server_action_forms>
</nextjs_form_patterns>

## API Integration

<nextjs_api_integration>
  <api_client_setup>
    <rule>Configure Axios with interceptors for automatic token management</rule>
    <code_example language="typescript">
// File: lib/api-client.ts
import axios from 'axios'
import { getSession, signIn } from 'next-auth/react'
import { API_BASE_URL } from '@/config/api-endpoints'

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor for auth
apiClient.interceptors.request.use(
  async (config) => {
    const session = await getSession()
    
    if (session?.accessToken) {
      config.headers.Authorization = `Bearer ${session.accessToken}`
    }
    
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      try {
        // Attempt to refresh token
        await signIn('refresh')
        return apiClient(originalRequest)
      } catch (refreshError) {
        // Redirect to login
        window.location.href = '/login'
        return Promise.reject(refreshError)
      }
    }

    return Promise.reject(error)
  }
)

// Typed API methods
export const api = {
  users: {
    getAll: () => apiClient.get<User[]>('/users'),
    getById: (id: string) => apiClient.get<User>(`/users/${id}`),
    create: (data: CreateUserDto) => apiClient.post<User>('/users', data),
    update: (id: string, data: UpdateUserDto) => apiClient.put<User>(`/users/${id}`, data),
    delete: (id: string) => apiClient.delete(`/users/${id}`)
  },
  
  orders: {
    getAll: (params?: OrderQueryParams) => apiClient.get<Order[]>('/orders', { params }),
    getById: (id: string) => apiClient.get<Order>(`/orders/${id}`),
    create: (data: CreateOrderDto) => apiClient.post<Order>('/orders', data),
    cancel: (id: string) => apiClient.post(`/orders/${id}/cancel`)
  }
}
</code_example>
  </api_client_setup>

  <api_routes>
    <rule>Create type-safe API routes</rule>
    <code_example language="typescript">
// File: app/api/users/route.ts
import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import { requireAuth } from '@/lib/auth-helpers'

const CreateUserSchema = z.object({
  email: z.string().email(),
  firstName: z.string().min(2),
  lastName: z.string().min(2),
  role: z.enum(['admin', 'user'])
})

export async function GET(request: NextRequest) {
  try {
    await requireAuth()
    
    const searchParams = request.nextUrl.searchParams
    const page = searchParams.get('page') || '1'
    const limit = searchParams.get('limit') || '10'
    
    const users = await db.user.findMany({
      skip: (parseInt(page) - 1) * parseInt(limit),
      take: parseInt(limit)
    })
    
    return NextResponse.json(users)
  } catch (error) {
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

export async function POST(request: NextRequest) {
  try {
    await requireAuth()
    
    const body = await request.json()
    const validatedData = CreateUserSchema.parse(body)
    
    const user = await db.user.create({
      data: validatedData
    })
    
    return NextResponse.json(user, { status: 201 })
  } catch (error) {
    if (error instanceof z.ZodError) {
      return NextResponse.json(
        { error: 'Validation failed', details: error.errors },
        { status: 400 }
      )
    }
    
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

// File: app/api/users/[id]/route.ts
export async function GET(
  request: NextRequest,
  { params }: { params: { id: string } }
) {
  try {
    await requireAuth()
    
    const user = await db.user.findUnique({
      where: { id: params.id }
    })
    
    if (!user) {
      return NextResponse.json(
        { error: 'User not found' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(user)
  } catch (error) {
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}
</code_example>
  </api_routes>
</nextjs_api_integration>

## Testing Patterns

<nextjs_testing_patterns>
  <component_testing>
    <rule>Test components with React Testing Library</rule>
    <code_example language="typescript">
// File: components/__tests__/user-card.test.tsx
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { UserCard } from '../user-card'

describe('UserCard', () => {
  const mockUser = {
    id: '1',
    name: 'John Doe',
    email: 'john@example.com',
    avatar: '/avatar.jpg'
  }

  it('renders user information', () => {
    render(<UserCard user={mockUser} />)
    
    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('john@example.com')).toBeInTheDocument()
    expect(screen.getByRole('img')).toHaveAttribute('src', '/avatar.jpg')
  })

  it('calls onClick when clicked', async () => {
    const handleClick = jest.fn()
    render(<UserCard user={mockUser} onClick={handleClick} />)
    
    const card = screen.getByRole('article')
    await userEvent.click(card)
    
    expect(handleClick).toHaveBeenCalledWith(mockUser)
  })

  it('shows loading skeleton when loading', () => {
    render(<UserCard loading />)
    
    expect(screen.getByTestId('user-card-skeleton')).toBeInTheDocument()
  })
})
</code_example>
  </component_testing>

  <integration_testing>
    <rule>Use Playwright for E2E testing</rule>
    <code_example language="typescript">
// File: tests/auth-flow.spec.ts
import { test, expect } from '@playwright/test'

test.describe('Authentication Flow', () => {
  test('user can login and access dashboard', async ({ page }) => {
    // Navigate to login page
    await page.goto('/login')
    
    // Fill in login form
    await page.fill('input[name="email"]', 'test@example.com')
    await page.fill('input[name="password"]', 'password123')
    
    // Submit form
    await page.click('button[type="submit"]')
    
    // Wait for redirect to dashboard
    await page.waitForURL('/dashboard')
    
    // Verify user is logged in
    await expect(page.locator('h1')).toContainText('Dashboard')
    await expect(page.locator('[data-testid="user-menu"]')).toBeVisible()
  })

  test('unauthenticated user is redirected to login', async ({ page }) => {
    // Try to access protected route
    await page.goto('/dashboard')
    
    // Should be redirected to login
    await expect(page).toHaveURL('/login?from=%2Fdashboard')
  })

  test('user can logout', async ({ page, context }) => {
    // Set authentication cookie
    await context.addCookies([
      {
        name: 'next-auth.session-token',
        value: 'valid-session-token',
        domain: 'localhost',
        path: '/'
      }
    ])
    
    // Navigate to dashboard
    await page.goto('/dashboard')
    
    // Click logout button
    await page.click('[data-testid="logout-button"]')
    
    // Should be redirected to home
    await expect(page).toHaveURL('/')
    
    // Verify logout message
    await expect(page.locator('[role="alert"]')).toContainText('Logged out successfully')
  })
})
</code_example>
  </integration_testing>

  <api_testing>
    <rule>Test API routes with integration tests</rule>
    <code_example language="typescript">
// File: app/api/__tests__/users.test.ts
import { GET, POST } from '../users/route'
import { NextRequest } from 'next/server'

jest.mock('@/lib/auth-helpers', () => ({
  requireAuth: jest.fn().mockResolvedValue({ userId: 'user-123' })
}))

describe('/api/users', () => {
  describe('GET', () => {
    it('returns list of users', async () => {
      const request = new NextRequest('http://localhost:3000/api/users')
      const response = await GET(request)
      const data = await response.json()
      
      expect(response.status).toBe(200)
      expect(Array.isArray(data)).toBe(true)
    })

    it('supports pagination', async () => {
      const request = new NextRequest('http://localhost:3000/api/users?page=2&limit=5')
      const response = await GET(request)
      const data = await response.json()
      
      expect(response.status).toBe(200)
      expect(data.length).toBeLessThanOrEqual(5)
    })
  })

  describe('POST', () => {
    it('creates a new user with valid data', async () => {
      const request = new NextRequest('http://localhost:3000/api/users', {
        method: 'POST',
        body: JSON.stringify({
          email: 'new@example.com',
          firstName: 'Jane',
          lastName: 'Doe',
          role: 'user'
        })
      })
      
      const response = await POST(request)
      const data = await response.json()
      
      expect(response.status).toBe(201)
      expect(data.email).toBe('new@example.com')
    })

    it('returns validation error for invalid data', async () => {
      const request = new NextRequest('http://localhost:3000/api/users', {
        method: 'POST',
        body: JSON.stringify({
          email: 'invalid-email',
          firstName: 'J'
        })
      })
      
      const response = await POST(request)
      const data = await response.json()
      
      expect(response.status).toBe(400)
      expect(data.error).toBe('Validation failed')
    })
  })
})
</code_example>
  </api_testing>
</nextjs_testing_patterns>

## Performance Optimization

<nextjs_performance_optimization>
  <image_optimization>
    <rule>Use Next.js Image component for automatic optimization</rule>
    <code_example language="typescript">
// File: components/product-card.tsx
import Image from 'next/image'

export function ProductCard({ product }: { product: Product }) {
  return (
    <div className="rounded-lg overflow-hidden shadow-lg">
      <div className="relative h-48 w-full">
        <Image
          src={product.image}
          alt={product.name}
          fill
          sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
          className="object-cover"
          priority={product.featured}
          placeholder="blur"
          blurDataURL={product.blurDataURL}
        />
      </div>
      <div className="p-4">
        <h3 className="font-semibold">{product.name}</h3>
        <p className="text-gray-600">${product.price}</p>
      </div>
    </div>
  )
}
</code_example>
  </image_optimization>

  <lazy_loading>
    <rule>Use dynamic imports for code splitting</rule>
    <code_example language="typescript">
// File: app/dashboard/page.tsx
import dynamic from 'next/dynamic'
import { Suspense } from 'react'

// Lazy load heavy components
const ChartComponent = dynamic(() => import('@/components/chart-component'), {
  loading: () => <div>Loading chart...</div>,
  ssr: false // Disable SSR for client-only components
})

const DataTable = dynamic(() => import('@/components/data-table'), {
  loading: () => <TableSkeleton />
})

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <Suspense fallback={<div>Loading...</div>}>
        <ChartComponent />
      </Suspense>
      
      <DataTable />
    </div>
  )
}
</code_example>
  </lazy_loading>

  <font_optimization>
    <rule>Optimize fonts with next/font</rule>
    <code_example language="typescript">
// File: app/layout.tsx
import { Inter, Roboto_Mono } from 'next/font/google'

const inter = Inter({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter'
})

const robotoMono = Roboto_Mono({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-roboto-mono'
})

export default function RootLayout({
  children
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className={`${inter.variable} ${robotoMono.variable}`}>
      <body className="font-sans">
        {children}
      </body>
    </html>
  )
}

// File: tailwind.config.ts
module.exports = {
  theme: {
    extend: {
      fontFamily: {
        sans: ['var(--font-inter)'],
        mono: ['var(--font-roboto-mono)']
      }
    }
  }
}
</code_example>
  </font_optimization>

  <memoization>
    <rule>Use React.memo and useMemo for expensive computations</rule>
    <code_example language="typescript">
// File: components/expense-chart.tsx
'use client'

import { memo, useMemo } from 'react'

interface ExpenseChartProps {
  data: ExpenseData[]
  dateRange: DateRange
}

export const ExpenseChart = memo(function ExpenseChart({ 
  data, 
  dateRange 
}: ExpenseChartProps) {
  const processedData = useMemo(() => {
    // Expensive data processing
    return data
      .filter(item => {
        const date = new Date(item.date)
        return date >= dateRange.start && date <= dateRange.end
      })
      .reduce((acc, item) => {
        const month = new Date(item.date).toLocaleString('default', { month: 'short' })
        acc[month] = (acc[month] || 0) + item.amount
        return acc
      }, {} as Record<string, number>)
  }, [data, dateRange])

  return (
    <div className="chart-container">
      {/* Render chart with processedData */}
    </div>
  )
}, (prevProps, nextProps) => {
  // Custom comparison for memo
  return (
    prevProps.dateRange.start === nextProps.dateRange.start &&
    prevProps.dateRange.end === nextProps.dateRange.end &&
    prevProps.data.length === nextProps.data.length
  )
})
</code_example>
  </memoization>

  <bundle_analysis>
    <rule>Analyze and optimize bundle size</rule>
    <code_example language="json">
// File: package.json
{
  "scripts": {
    "analyze": "ANALYZE=true next build"
  }
}
</code_example>
    <code_example language="typescript">
// File: next.config.js
const withBundleAnalyzer = require('@next/bundle-analyzer')({
  enabled: process.env.ANALYZE === 'true'
})

module.exports = withBundleAnalyzer({
  // Optimize imports
  modularizeImports: {
    'lodash': {
      transform: 'lodash/{{member}}'
    },
    '@mui/material': {
      transform: '@mui/material/{{member}}'
    }
  },
  
  // Production optimizations
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production'
  }
})
</code_example>
  </bundle_analysis>
</nextjs_performance_optimization>

## Build and Deployment

<nextjs_build_deployment>
  <build_configuration>
    <rule>Configure Next.js for production builds</rule>
    <code_example language="javascript">
// File: next.config.js
/** @type {import('next').NextConfig} */
const nextConfig = {
  // Output configuration
  output: 'standalone', // For Docker deployments
  
  // Image optimization
  images: {
    domains: ['cdn.example.com'],
    formats: ['image/avif', 'image/webp'],
    minimumCacheTTL: 60
  },
  
  // Security headers
  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          {
            key: 'X-Frame-Options',
            value: 'DENY'
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff'
          },
          {
            key: 'X-XSS-Protection',
            value: '1; mode=block'
          }
        ]
      }
    ]
  },
  
  // Redirects
  async redirects() {
    return [
      {
        source: '/old-route',
        destination: '/new-route',
        permanent: true
      }
    ]
  },
  
  // Environment variables
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL
  }
}

module.exports = nextConfig
</code_example>
  </build_configuration>

  <docker_deployment>
    <rule>Create optimized Docker image for Next.js</rule>
    <code_example language="dockerfile">
# File: Dockerfile
# Dependencies
FROM node:20-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

COPY package.json pnpm-lock.yaml ./
RUN corepack enable pnpm && pnpm i --frozen-lockfile

# Builder
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

ENV NEXT_TELEMETRY_DISABLED 1
RUN corepack enable pnpm && pnpm build

# Runner
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV production
ENV NEXT_TELEMETRY_DISABLED 1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public

# Leverage output traces to reduce image size
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000

ENV PORT 3000

CMD ["node", "server.js"]
</code_example>
  </docker_deployment>

  <environment_variables>
    <rule>Manage environment variables properly</rule>
    <code_example language="typescript">
// File: lib/env-config.ts
import { z } from 'zod'

const envSchema = z.object({
  // Public variables (exposed to client)
  NEXT_PUBLIC_API_URL: z.string().url(),
  NEXT_PUBLIC_APP_NAME: z.string().default('My App'),
  NEXT_PUBLIC_GA_ID: z.string().optional(),
  
  // Server-only variables
  DATABASE_URL: z.string(),
  JWT_SECRET: z.string().min(32),
  SMTP_HOST: z.string(),
  SMTP_PORT: z.string().transform(Number),
  SMTP_USER: z.string(),
  SMTP_PASS: z.string()
})

// Validate environment variables at build time
export const env = envSchema.parse(process.env)

// Type-safe environment variable access
declare global {
  namespace NodeJS {
    interface ProcessEnv extends z.infer<typeof envSchema> {}
  }
}
</code_example>
  </environment_variables>

  <deployment_checklist>
    <rule>Follow deployment best practices</rule>
    <checklist>
      <item>Run next build locally to catch build errors</item>
      <item>Check bundle size with next-bundle-analyzer</item>
      <item>Test with production environment variables</item>
      <item>Configure proper caching headers</item>
      <item>Set up error monitoring (Sentry)</item>
      <item>Configure logging (Winston, Pino)</item>
      <item>Set up health check endpoint</item>
      <item>Configure rate limiting</item>
      <item>Enable CORS if needed</item>
      <item>Set up CDN for static assets</item>
      <item>Configure database connection pooling</item>
      <item>Set up monitoring and alerts</item>
    </checklist>
  </deployment_checklist>
</nextjs_build_deployment>

## Error Handling

<nextjs_error_handling>
  <error_boundaries>
    <rule>Use error.tsx files for error boundaries</rule>
    <code_example language="typescript">
// File: app/error.tsx
'use client'

import { useEffect } from 'react'
import { Button } from '@/components/ui/button'

export default function Error({
  error,
  reset
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    // Log error to error reporting service
    console.error(error)
  }, [error])

  return (
    <div className="flex min-h-screen flex-col items-center justify-center">
      <h2 className="text-2xl font-bold mb-4">Something went wrong!</h2>
      <p className="text-gray-600 mb-6">
        {error.message || 'An unexpected error occurred'}
      </p>
      <Button onClick={reset}>Try again</Button>
    </div>
  )
}

// File: app/dashboard/error.tsx
'use client'

export default function DashboardError({
  error,
  reset
}: {
  error: Error
  reset: () => void
}) {
  return (
    <div className="rounded-lg bg-red-50 p-4">
      <h3 className="font-semibold text-red-800">Dashboard Error</h3>
      <p className="text-red-600 mt-2">{error.message}</p>
      <button
        onClick={reset}
        className="mt-4 px-4 py-2 bg-red-600 text-white rounded"
      >
        Retry
      </button>
    </div>
  )
}
</code_example>
  </error_boundaries>

  <not_found_pages>
    <rule>Create custom not-found pages</rule>
    <code_example language="typescript">
// File: app/not-found.tsx
import Link from 'next/link'
import { Button } from '@/components/ui/button'

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center">
      <h1 className="text-6xl font-bold mb-4">404</h1>
      <h2 className="text-2xl mb-4">Page Not Found</h2>
      <p className="text-gray-600 mb-8">
        The page you're looking for doesn't exist.
      </p>
      <Button asChild>
        <Link href="/">Go Home</Link>
      </Button>
    </div>
  )
}

// File: app/products/[id]/not-found.tsx
export default function ProductNotFound() {
  return (
    <div className="text-center py-10">
      <h2 className="text-xl font-semibold">Product Not Found</h2>
      <p className="mt-2 text-gray-600">
        The product you're looking for doesn't exist or has been removed.
      </p>
    </div>
  )
}
</code_example>
  </not_found_pages>
</nextjs_error_handling>

## Best Practices Summary

<nextjs_best_practices>
  <key_principles>
    <principle>Use kebab-case for all files and folders for clean URLs</principle>
    <principle>Default to Server Components, use Client Components only when needed</principle>
    <principle>Implement proper error boundaries and loading states</principle>
    <principle>Use Server Actions for forms when possible</principle>
    <principle>Optimize images, fonts, and bundle size</principle>
    <principle>Implement proper authentication with middleware</principle>
    <principle>Use TypeScript with strict mode enabled</principle>
    <principle>Follow React and Next.js best practices</principle>
    <principle>Write comprehensive tests (unit, integration, E2E)</principle>
    <principle>Properly handle environment variables</principle>
  </key_principles>

  <common_pitfalls>
    <pitfall>
      <issue>Using Client Components unnecessarily</issue>
      <solution>Default to Server Components, only use 'use client' when you need interactivity</solution>
    </pitfall>
    <pitfall>
      <issue>Not using proper file naming conventions</issue>
      <solution>Always use kebab-case for files and folders in Next.js projects</solution>
    </pitfall>
    <pitfall>
      <issue>Fetching data in Client Components instead of Server Components</issue>
      <solution>Fetch data in Server Components when possible for better performance</solution>
    </pitfall>
    <pitfall>
      <issue>Not handling loading and error states</issue>
      <solution>Always implement loading.tsx and error.tsx files</solution>
    </pitfall>
    <pitfall>
      <issue>Exposing sensitive environment variables to the client</issue>
      <solution>Only prefix with NEXT_PUBLIC_ variables that should be exposed to the client</solution>
    </pitfall>
  </common_pitfalls>
</nextjs_best_practices>

## Migration from Vite to Next.js

<vite_to_nextjs_migration>
  <key_differences>
    <difference category="routing">
      <vite>React Router with manual route configuration</vite>
      <nextjs>File-based routing with App Router</nextjs>
    </difference>
    <difference category="data_fetching">
      <vite>Client-side fetching with useEffect or libraries</vite>
      <nextjs>Server Components with direct async data fetching</nextjs>
    </difference>
    <difference category="ssr">
      <vite>Client-side rendering by default</vite>
      <nextjs>Server-side rendering by default</nextjs>
    </difference>
    <difference category="api_routes">
      <vite>Separate backend needed</vite>
      <nextjs>Built-in API routes in /app/api</nextjs>
    </difference>
    <difference category="environment_variables">
      <vite>VITE_ prefix for public variables</vite>
      <nextjs>NEXT_PUBLIC_ prefix for public variables</nextjs>
    </difference>
  </key_differences>

  <migration_steps>
    <step order="1">Update file structure to App Router conventions</step>
    <step order="2">Convert React Router to file-based routing</step>
    <step order="3">Move API calls to Server Components where possible</step>
    <step order="4">Update environment variable prefixes</step>
    <step order="5">Convert client-side data fetching to Server Components</step>
    <step order="6">Update build and deployment configurations</step>
    <step order="7">Migrate tests to Next.js testing patterns</step>
  </migration_steps>
</vite_to_nextjs_migration>

<conclusion>
This guide provides comprehensive patterns and best practices for Next.js development with kebab-case naming conventions. Follow these patterns to build scalable, maintainable, and performant Next.js applications.
</conclusion>