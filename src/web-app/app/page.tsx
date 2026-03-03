'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useAuth } from '@/contexts/auth-context';

export default function HomePage() {
  const { isAuthenticated, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) {
      return;
    }

    router.replace(isAuthenticated ? '/dashboard' : '/login');
  }, [isAuthenticated, isLoading, router]);

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        px: 3,
      }}
    >
      <Stack spacing={2} alignItems="center">
        <CircularProgress size={28} />
        <Typography color="text.secondary">
          Resolving your session...
        </Typography>
      </Stack>
    </Box>
  );
}
