import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from '@/components/ui/sonner';
import { AuthProvider, useAuth } from '@/contexts/AuthContext';
import { ThemeProvider } from '@/context/theme-provider';
import { SearchProvider } from '@/contexts/SearchProvider';
import { LayoutProvider } from '@/context/layout-provider';
import { NavigationProgress } from '@/components/navigation-progress';
import { NotFoundError } from '@/features/errors/not-found-error';

// Features
import { SignIn } from '@/features/auth/sign-in';
import { Dashboard } from '@/features/dashboard';

// Layout
import { AuthenticatedLayout } from '@/components/layout/authenticated-layout';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/sign-in" replace />;
  }

  return (
    <SearchProvider>
      <LayoutProvider>
        <AuthenticatedLayout>{children}</AuthenticatedLayout>
      </LayoutProvider>
    </SearchProvider>
  );
}

function AppRoutes() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <Routes>
      {/* Public routes */}
      <Route 
        path="/sign-in" 
        element={isAuthenticated ? <Navigate to="/" replace /> : <SignIn />} 
      />
      
      {/* Protected routes */}
      <Route 
        path="/" 
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        } 
      />

      {/* Redirect old routes */}
      <Route 
        path="/login" 
        element={<Navigate to="/sign-in" replace />} 
      />
      <Route 
        path="/dashboard" 
        element={<Navigate to="/" replace />} 
      />

      {/* 404 route */}
      <Route 
        path="*" 
        element={<NotFoundError />}
      />
    </Routes>
  );
}

function App() {
  return (
    <ThemeProvider defaultTheme="system" storageKey="ui-theme">
      <Router>
        <AuthProvider>
          <NavigationProgress />
          <AppRoutes />
          <Toaster duration={5000} />
        </AuthProvider>
      </Router>
    </ThemeProvider>
  );
}

export default App;