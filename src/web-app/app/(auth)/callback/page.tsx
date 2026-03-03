'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';

import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { AuthScreenShell } from '@/components/auth';
import { useAuth } from '@/contexts/auth-context';
import OAuth2Auth from '@/lib/auth/oauth2-auth';

export default function CallbackPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let isActive = true;

    async function completeCallback() {
      try {
        const result = await OAuth2Auth.handleCallback();
        const returnUrl = sessionStorage.getItem('returnUrl') || '/dashboard';

        sessionStorage.removeItem('returnUrl');
        login(result.user);

        if (isActive) {
          router.replace(returnUrl);
        }
      } catch (error) {
        const message = error instanceof Error
          ? error.message
          : 'Authentication failed. Please try signing in again.';

        if (isActive) {
          setErrorMessage(message);
        }
      }
    }

    void completeCallback();

    return () => {
      isActive = false;
    };
  }, [login, router]);

  return (
    <AuthScreenShell
      badge={errorMessage ? 'Authentication failed' : 'Completing sign-in'}
      title={errorMessage ? 'We could not complete sign-in' : 'Restoring your workspace'}
      description={errorMessage
        ? 'The OAuth callback returned an error or could not finish token exchange. You can return to the sign-in page and try again.'
        : 'The application is finalizing your sign-in, storing your session, and taking you back to the correct screen.'}
    >
      <Stack spacing={3} alignItems="flex-start">
        {errorMessage ? (
          <>
            <Typography color="text.secondary">
              {errorMessage}
            </Typography>
            <Button
              variant="contained"
              onClick={() => router.replace('/login')}
            >
              Back to login
            </Button>
          </>
        ) : (
          <>
            <Stack direction="row" spacing={1.5} alignItems="center">
              <CircularProgress size={28} />
              <Typography variant="h6" fontWeight={700}>
                Completing sign-in
              </Typography>
            </Stack>
            <Typography color="text.secondary">
              Please wait while your account access is restored.
            </Typography>
          </>
        )}
      </Stack>
    </AuthScreenShell>
  );
}
