'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import OAuth2Auth from '@/lib/oauth2-auth';
import { useAuth } from '@/contexts/auth-context';

export default function CallbackPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Handle the OAuth2 callback
        const result = await OAuth2Auth.handleCallback();

        // Update auth context
        login(result.user, result.accessToken);

        // Redirect to the intended page or dashboard
        const returnUrl = sessionStorage.getItem('returnUrl') || '/dashboard';
        sessionStorage.removeItem('returnUrl');
        router.push(returnUrl);
      } catch (err) {
        console.error('OAuth2 callback error:', err);
        setError(err instanceof Error ? err.message : 'Authentication failed');

        // Redirect to login after a delay
        setTimeout(() => {
          router.push('/login');
        }, 3000);
      }
    };

    handleCallback();
  }, [router, login]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-100">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full">
          <h2 className="text-2xl font-bold text-red-600 mb-4">Authentication Error</h2>
          <p className="text-gray-700 mb-4">{error}</p>
          <p className="text-sm text-gray-500">Redirecting to login...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md">
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <span className="text-lg text-gray-700">Completing authentication...</span>
        </div>
      </div>
    </div>
  );
}