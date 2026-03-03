'use client';

import { alpha } from '@mui/material/styles';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { Icon } from '@iconify/react';

import { AccountMenu } from './account-menu';
import { ThemeToggleButton } from './theme-toggle-button';

type AppHeaderProps = {
  title: string;
  subtitle: string;
  onOpenMobileNav: () => void;
};

export function AppHeader({ title, subtitle, onOpenMobileNav }: AppHeaderProps) {
  return (
    <Paper
      component="header"
      elevation={0}
      sx={{
        px: { xs: 2, sm: 3, lg: 4 },
        py: 2,
        borderBottom: '1px solid',
        borderColor: 'divider',
        borderRadius: 0,
        bgcolor: 'background.paper',
      }}
    >
      <Stack
        direction="row"
        spacing={2}
        alignItems="center"
        justifyContent="space-between"
      >
        <Stack direction="row" spacing={1.5} alignItems="center">
          <IconButton
            color="inherit"
            aria-label="Open navigation"
            onClick={onOpenMobileNav}
            sx={{ display: { lg: 'none' } }}
          >
            <Icon icon="solar:hamburger-menu-bold-duotone" width={22} />
          </IconButton>

          <Stack spacing={0.25}>
            <Typography variant="h5" fontWeight={700}>
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          </Stack>
        </Stack>

        <Stack direction="row" spacing={1} alignItems="center">
          <Box
            sx={{
              display: { xs: 'none', md: 'flex' },
              alignItems: 'center',
              gap: 1,
              minWidth: 220,
              px: 1.5,
              py: 1,
              borderRadius: 99,
              border: '1px solid',
              borderColor: 'divider',
              color: 'text.secondary',
              bgcolor: (theme) => alpha(theme.palette.background.default, 0.7),
            }}
          >
            <Icon icon="solar:magnifer-linear" width={18} />
            <Typography variant="body2" sx={{ flex: 1 }}>
              Search dashboard
            </Typography>
            <Typography variant="caption" fontWeight={700}>
              ⌘K
            </Typography>
          </Box>

          <ThemeToggleButton />
          <AccountMenu />
        </Stack>
      </Stack>
    </Paper>
  );
}
