'use client';

import {
  createTheme,
  responsiveFontSizes,
  type PaletteMode,
  type ThemeOptions,
} from '@mui/material/styles';

function getThemeOptions(mode: PaletteMode, fontFamily: string): ThemeOptions {
  const isLight = mode === 'light';

  return {
    palette: {
      mode,
      primary: {
        main: '#2065D1',
        light: '#5B8DEF',
        dark: '#123A8C',
        contrastText: '#FFFFFF',
      },
      secondary: {
        main: '#00A76F',
        light: '#5BE49B',
        dark: '#007867',
      },
      success: {
        main: '#22C55E',
      },
      warning: {
        main: '#FFAB00',
      },
      error: {
        main: '#FF5630',
      },
      background: {
        default: isLight ? '#F4F6FB' : '#0B1020',
        paper: isLight ? '#FFFFFF' : '#111827',
      },
      text: {
        primary: isLight ? '#1C252E' : '#F9FAFB',
        secondary: isLight ? '#637381' : '#9CA3AF',
      },
      divider: isLight ? 'rgba(145, 158, 171, 0.20)' : 'rgba(145, 158, 171, 0.16)',
    },
    shape: {
      borderRadius: 20,
    },
    typography: {
      fontFamily,
      h1: {
        fontSize: '3.5rem',
        fontWeight: 800,
        lineHeight: 1.08,
        letterSpacing: '-0.03em',
      },
      h2: {
        fontSize: '2.75rem',
        fontWeight: 800,
        lineHeight: 1.1,
        letterSpacing: '-0.025em',
      },
      h3: {
        fontSize: '2.125rem',
        fontWeight: 800,
        lineHeight: 1.12,
        letterSpacing: '-0.02em',
      },
      h4: {
        fontSize: '1.75rem',
        fontWeight: 800,
        lineHeight: 1.16,
        letterSpacing: '-0.015em',
      },
      h5: {
        fontSize: '1.375rem',
        fontWeight: 700,
        lineHeight: 1.24,
      },
      h6: {
        fontSize: '1rem',
        fontWeight: 700,
        lineHeight: 1.3,
      },
      subtitle1: {
        fontWeight: 700,
      },
      subtitle2: {
        fontWeight: 700,
      },
      body1: {
        lineHeight: 1.65,
      },
      body2: {
        lineHeight: 1.6,
      },
      button: {
        fontWeight: 700,
        textTransform: 'none',
        letterSpacing: 0,
      },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundImage: isLight
              ? 'radial-gradient(circle at top right, rgba(32, 101, 209, 0.08), transparent 28%), radial-gradient(circle at bottom left, rgba(0, 167, 111, 0.05), transparent 24%)'
              : 'radial-gradient(circle at top right, rgba(32, 101, 209, 0.18), transparent 28%), radial-gradient(circle at bottom left, rgba(0, 167, 111, 0.08), transparent 24%)',
          },
        },
      },
      MuiButton: {
        defaultProps: {
          disableElevation: true,
        },
        styleOverrides: {
          root: {
            borderRadius: 14,
            paddingInline: 18,
          },
          containedPrimary: {
            boxShadow: isLight
              ? '0 8px 18px rgba(32, 101, 209, 0.22)'
              : '0 10px 24px rgba(32, 101, 209, 0.24)',
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 24,
            border: '1px solid',
            borderColor: isLight ? 'rgba(145, 158, 171, 0.18)' : 'rgba(145, 158, 171, 0.12)',
            boxShadow: isLight
              ? '0 12px 32px rgba(145, 158, 171, 0.16)'
              : '0 12px 32px rgba(2, 6, 23, 0.34)',
            backgroundImage: 'none',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
          rounded: {
            borderRadius: 24,
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: 12,
          },
        },
      },
      MuiIconButton: {
        styleOverrides: {
          root: {
            borderRadius: 14,
          },
        },
      },
      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: 16,
          },
        },
      },
    },
  };
}

export function createAppTheme(mode: PaletteMode, fontFamily: string) {
  return responsiveFontSizes(createTheme(getThemeOptions(mode, fontFamily)));
}
