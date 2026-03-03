'use client';

import type { ReactNode } from 'react';
import Image from 'next/image';

import { alpha } from '@mui/material/styles';
import Box from '@mui/material/Box';
import Chip from '@mui/material/Chip';
import Container from '@mui/material/Container';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

type AuthScreenShellProps = {
  badge: string;
  title: string;
  description: string;
  children: ReactNode;
};

export function AuthScreenShell({
  badge,
  title,
  description,
  children,
}: AuthScreenShellProps) {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        position: 'relative',
        overflow: 'hidden',
        bgcolor: 'background.default',
        backgroundImage: (theme) => theme.palette.mode === 'dark'
          ? `radial-gradient(circle at top left, ${alpha(theme.palette.primary.main, 0.16)}, transparent 42%)`
          : `radial-gradient(circle at top left, ${alpha(theme.palette.primary.main, 0.1)}, transparent 42%)`,
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          backgroundImage: (theme) => `
            linear-gradient(135deg, ${alpha(theme.palette.background.paper, 0.36)}, transparent),
            linear-gradient(0deg, ${alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.08 : 0.04)}, transparent)
          `,
          pointerEvents: 'none',
        }}
      />

      <Container
        maxWidth="xl"
        sx={{
          position: 'relative',
          py: { xs: 3, md: 4 },
          minHeight: '100vh',
          display: 'grid',
          alignItems: 'center',
        }}
      >
        <Box
          sx={{
            display: 'grid',
            gap: { xs: 3, lg: 4 },
            gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1.1fr) minmax(420px, 520px)' },
            alignItems: 'stretch',
          }}
        >
          <Paper
            elevation={0}
            sx={{
              position: 'relative',
              display: { xs: 'none', lg: 'flex' },
              borderRadius: 5,
              p: 5,
              minHeight: 640,
              overflow: 'hidden',
              border: 1,
              borderColor: 'divider',
              backgroundImage: (theme) => `
                linear-gradient(160deg, ${alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.22 : 0.1)}, transparent 58%),
                linear-gradient(340deg, ${alpha(theme.palette.info.main, theme.palette.mode === 'dark' ? 0.16 : 0.08)}, transparent 50%)
              `,
              bgcolor: 'background.paper',
            }}
          >
            <Stack
              spacing={4}
              justifyContent="space-between"
              sx={{ width: '100%', position: 'relative', zIndex: 1 }}
            >
              <Stack spacing={2.5} maxWidth={520}>
                <Chip
                  label="Identity Vibe"
                  color="primary"
                  sx={{ alignSelf: 'flex-start', fontWeight: 700 }}
                />
                <Stack spacing={1.5}>
                  <Typography variant="h2" sx={{ fontWeight: 800, lineHeight: 1.08 }}>
                    Modern admin tools for secure operations
                  </Typography>
                  <Typography variant="h6" color="text.secondary" sx={{ maxWidth: 480 }}>
                    Manage orders, users, and permissions from a cleaner
                    dashboard experience built for the next version of the admin app.
                  </Typography>
                </Stack>
              </Stack>

              <Box
                sx={{
                  position: 'relative',
                  alignSelf: 'center',
                  width: '100%',
                  maxWidth: 620,
                  aspectRatio: '16 / 10',
                }}
              >
                <Image
                  src="/images/auth/dashboard-illustration.webp"
                  alt="Dashboard illustration"
                  fill
                  priority
                  sizes="(min-width: 1200px) 620px, 0px"
                  style={{ objectFit: 'contain' }}
                />
              </Box>
            </Stack>
          </Paper>

          <Paper
            elevation={0}
            sx={{
              borderRadius: { xs: 4, md: 5 },
              border: 1,
              borderColor: 'divider',
              bgcolor: 'background.paper',
              px: { xs: 3, sm: 4.5, lg: 5 },
              py: { xs: 4, sm: 5 },
              display: 'flex',
              alignItems: 'center',
            }}
          >
            <Stack spacing={4} sx={{ width: '100%' }}>
              <Stack spacing={2}>
                <Chip
                  label={badge}
                  variant="outlined"
                  color="primary"
                  sx={{ alignSelf: 'flex-start', fontWeight: 700 }}
                />
                <Stack spacing={1.5}>
                  <Typography variant="h3" sx={{ fontWeight: 800, lineHeight: 1.1 }}>
                    {title}
                  </Typography>
                  <Typography color="text.secondary">
                    {description}
                  </Typography>
                </Stack>
              </Stack>

              {children}
            </Stack>
          </Paper>
        </Box>
      </Container>
    </Box>
  );
}
