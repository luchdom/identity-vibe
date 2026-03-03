'use client';

import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';

import { Icon } from '@iconify/react';

import { useThemeMode } from '@/theme';

export function ThemeToggleButton() {
  const { mode, toggleColorMode } = useThemeMode();

  return (
    <Tooltip title={mode === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}>
      <IconButton color="inherit" onClick={toggleColorMode} aria-label="Toggle color mode">
        <Icon
          icon={mode === 'light'
            ? 'solar:moon-stars-bold-duotone'
            : 'solar:sun-2-bold-duotone'}
          width={22}
        />
      </IconButton>
    </Tooltip>
  );
}
