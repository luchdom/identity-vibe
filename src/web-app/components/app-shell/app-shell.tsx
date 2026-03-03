'use client';

import { useState, type ReactNode } from 'react';

import Box from '@mui/material/Box';

import { AppHeader } from './header';
import { AppSidebar } from './sidebar';

type AppShellProps = {
  title: string;
  subtitle: string;
  children: ReactNode;
};

export function AppShell({ title, subtitle, children }: AppShellProps) {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppSidebar
        mobileOpen={mobileOpen}
        onCloseMobile={() => setMobileOpen(false)}
      />

      <Box
        sx={{
          flex: 1,
          minWidth: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <AppHeader
          title={title}
          subtitle={subtitle}
          onOpenMobileNav={() => setMobileOpen(true)}
        />

        <Box
          component="main"
          sx={{
            flex: 1,
            p: { xs: 2, sm: 3, lg: 4 },
          }}
        >
          <Box sx={{ maxWidth: 1440, mx: 'auto' }}>
            {children}
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
