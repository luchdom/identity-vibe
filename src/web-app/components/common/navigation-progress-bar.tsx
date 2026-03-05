'use client';

import Box from '@mui/material/Box';
import LinearProgress from '@mui/material/LinearProgress';
import { alpha } from '@mui/material/styles';

type NavigationProgressBarProps = {
  visible: boolean;
};

export function NavigationProgressBar({ visible }: NavigationProgressBarProps) {
  return (
    <Box
      aria-hidden={!visible}
      sx={{
        position: 'absolute',
        left: 0,
        right: 0,
        bottom: 0,
        height: 2,
        opacity: visible ? 1 : 0,
        pointerEvents: 'none',
        transition: 'opacity 180ms ease',
        overflow: 'hidden',
        zIndex: 1,
      }}
    >
      <LinearProgress
        sx={{
          height: '100%',
          borderRadius: 0,
          bgcolor: 'transparent',
          '& .MuiLinearProgress-bar': {
            bgcolor: 'primary.main',
            boxShadow: (theme) => `0 0 8px ${alpha(theme.palette.primary.main, 0.45)}`,
          },
        }}
      />
    </Box>
  );
}

