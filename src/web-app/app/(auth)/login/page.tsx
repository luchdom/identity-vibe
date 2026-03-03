'use client';

import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { AuthGuard, AuthScreenShell } from '@/components/auth';
import { useAuth } from '@/contexts/auth-context';

export default function LoginPage() {
  const { authorize } = useAuth();

  return (
    <AuthGuard requireAuth={false}>
      <AuthScreenShell
        badge="Sign in"
        title="Access the new admin workspace"
        description="Use your existing AuthServer account to continue. Sign-in uses the same secure OAuth2 PKCE flow, now presented in the updated Material-based workspace."
      >
        <Stack spacing={3.5}>
          <Stack spacing={1.25}>
            <Typography variant="body1" color="text.secondary">
              Your account and access model stay the same. This updated interface
              focuses on a cleaner admin experience without changing the sign-in flow.
            </Typography>
            <Typography variant="body2" color="text.secondary">
              After sign-in, the app will return you to your original destination
              when a protected route triggered the login flow.
            </Typography>
          </Stack>

          <Button
            variant="contained"
            size="large"
            onClick={() => {
              void authorize();
            }}
            sx={{ py: 1.5, fontWeight: 700 }}
          >
            Continue with AuthServer
          </Button>

          <Divider />

          <Typography variant="body2" color="text.secondary">
            Dedicated OAuth client: <strong>web-app</strong>
          </Typography>
        </Stack>
      </AuthScreenShell>
    </AuthGuard>
  );
}
