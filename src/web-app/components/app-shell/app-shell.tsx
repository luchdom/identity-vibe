'use client';

import { useEffect, useState, type ReactNode } from 'react';

import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';

import { Icon } from '@iconify/react';

import { AppHeader } from './header';
import {
  AppSidebar,
  appSidebarCollapsedWidth,
  appSidebarWidth,
} from './sidebar';

const DESKTOP_SIDEBAR_STORAGE_KEY = 'web-app-sidebar-expanded';

type AppShellProps = {
  title: string;
  subtitle: string;
  children: ReactNode;
};

export function AppShell({ title, subtitle, children }: AppShellProps) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [desktopSidebarExpanded, setDesktopSidebarExpanded] = useState(true);
  const [hasLoadedDesktopSidebarPreference, setHasLoadedDesktopSidebarPreference] = useState(false);

  useEffect(() => {
    setDesktopSidebarExpanded(
      window.localStorage.getItem(DESKTOP_SIDEBAR_STORAGE_KEY) !== 'false'
    );
    setHasLoadedDesktopSidebarPreference(true);
  }, []);

  useEffect(() => {
    if (!hasLoadedDesktopSidebarPreference) {
      return;
    }

    window.localStorage.setItem(
      DESKTOP_SIDEBAR_STORAGE_KEY,
      String(desktopSidebarExpanded)
    );
  }, [desktopSidebarExpanded, hasLoadedDesktopSidebarPreference]);

  return (
    <Box
      sx={{
        position: 'relative',
        display: 'flex',
        minHeight: '100vh',
        bgcolor: 'background.default',
      }}
    >
      <AppSidebar
        mobileOpen={mobileOpen}
        onCloseMobile={() => setMobileOpen(false)}
        desktopExpanded={desktopSidebarExpanded}
      />

      <Tooltip title={desktopSidebarExpanded ? 'Collapse sidebar' : 'Expand sidebar'} placement="right">
        <IconButton
          size="small"
          aria-label={desktopSidebarExpanded ? 'Collapse sidebar' : 'Expand sidebar'}
          onClick={() => setDesktopSidebarExpanded((current) => !current)}
          sx={{
            position: 'fixed',
            top: 24,
            left: desktopSidebarExpanded ? appSidebarWidth : appSidebarCollapsedWidth,
            transform: 'translateX(-50%)',
            zIndex: (theme) => theme.zIndex.drawer + 1,
            display: { xs: 'none', lg: 'inline-flex' },
            width: 24,
            height: 24,
            border: '1px solid',
            borderColor: 'divider',
            bgcolor: 'background.paper',
            color: 'text.secondary',
            boxShadow: '0 4px 12px rgba(15, 23, 42, 0.12)',
            '&:hover': {
              bgcolor: 'background.paper',
              color: 'text.primary',
              borderColor: 'text.secondary',
            },
          }}
        >
          <Icon
            icon={desktopSidebarExpanded
              ? 'solar:alt-arrow-left-line-duotone'
              : 'solar:alt-arrow-right-line-duotone'}
            width={14}
          />
        </IconButton>
      </Tooltip>

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
