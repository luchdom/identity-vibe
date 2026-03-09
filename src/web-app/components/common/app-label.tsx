import type { ComponentPropsWithoutRef, ReactNode } from 'react';

import Box from '@mui/material/Box';
import { alpha, useTheme } from '@mui/material/styles';
import Typography from '@mui/material/Typography';

type AppLabelColor = 'default' | 'success' | 'warning' | 'error' | 'info' | 'primary';

type AppLabelVariant = 'soft' | 'filled';

type AppLabelProps = {
  children: ReactNode;
  color?: AppLabelColor;
  variant?: AppLabelVariant;
} & ComponentPropsWithoutRef<'span'>;

export function AppLabel({
  children,
  color = 'default',
  variant = 'soft',
  ...rest
}: AppLabelProps) {
  const theme = useTheme();

  const palette = color === 'default'
    ? {
        main: theme.palette.text.primary,
        contrastText: theme.palette.common.white,
        softText: theme.palette.text.secondary,
      }
    : {
        main: theme.palette[color].main,
        contrastText: theme.palette[color].contrastText,
        softText: theme.palette.mode === 'light'
          ? theme.palette[color].dark
          : theme.palette[color].light,
      };

  const softBackground = color === 'default'
    ? alpha(theme.palette.grey[500], 0.16)
    : alpha(palette.main, 0.16);

  return (
    <Box
      component="span"
      {...rest}
      sx={{
        height: 24,
        minWidth: 24,
        px: 0.75,
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: 0.75,
        whiteSpace: 'nowrap',
        transition: theme.transitions.create(['background-color', 'color'], {
          duration: theme.transitions.duration.shorter,
        }),
        bgcolor: variant === 'filled' ? palette.main : softBackground,
        color: variant === 'filled' ? palette.contrastText : palette.softText,
      }}
    >
      <Typography
        component="span"
        sx={{
          fontSize: 12,
          lineHeight: 1,
          fontWeight: 700,
        }}
      >
        {children}
      </Typography>
    </Box>
  );
}
