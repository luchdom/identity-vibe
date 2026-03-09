'use client';

import { useCallback, useRef } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';

import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import { alpha } from '@mui/material/styles';

import { Icon } from '@iconify/react';

import { primaryNavItems } from './nav-config';
import { useNavigationProgress } from '@/contexts/navigation-progress-context';

const drawerWidth = 296;
const collapsedDrawerWidth = 92;

type SidebarContentProps = {
  onNavigate?: () => void;
  collapsed?: boolean;
};

function SidebarContent({ onNavigate, collapsed = false }: SidebarContentProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { isNavigating, startNavigation } = useNavigationProgress();
  const prefetchedRoutesRef = useRef<Set<string>>(new Set());

  const prefetchRoute = useCallback((href: string) => {
    if (prefetchedRoutesRef.current.has(href)) {
      return;
    }

    prefetchedRoutesRef.current.add(href);
    router.prefetch(href);
  }, [router]);

  return (
    <Stack
      sx={{
        height: '100%',
        px: collapsed ? 1 : 2,
        py: 2.5,
        bgcolor: 'background.paper',
      }}
    >
      <Tooltip title="Identity Vibe" placement="right" disableHoverListener={!collapsed}>
        <Paper
          elevation={0}
          sx={{
            p: collapsed ? 1.25 : 2,
            border: 1,
            borderColor: 'divider',
            background: (theme) => theme.palette.mode === 'light'
              ? `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.10)}, rgba(0, 167, 111, 0.04))`
              : `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.24)}, rgba(0, 167, 111, 0.10))`,
          }}
        >
          {collapsed ? (
            <Box
              sx={{
                display: 'grid',
                placeItems: 'center',
              }}
            >
              <Box
                sx={{
                  display: 'grid',
                  placeItems: 'center',
                  width: 42,
                  height: 42,
                  borderRadius: 3,
                  bgcolor: 'primary.main',
                  color: 'primary.contrastText',
                  boxShadow: (theme) => `0 12px 24px ${alpha(theme.palette.primary.main, 0.28)}`,
                }}
              >
                <Icon icon="solar:shield-keyhole-bold-duotone" width={22} />
              </Box>
            </Box>
          ) : (
            <Stack spacing={2}>
              <Stack direction="row" spacing={1.5} alignItems="center">
                <Box
                  sx={{
                    display: 'grid',
                    placeItems: 'center',
                    width: 46,
                    height: 46,
                    borderRadius: 3,
                    bgcolor: 'primary.main',
                    color: 'primary.contrastText',
                    boxShadow: (theme) => `0 12px 24px ${alpha(theme.palette.primary.main, 0.28)}`,
                  }}
                >
                  <Icon icon="solar:shield-keyhole-bold-duotone" width={24} />
                </Box>
                <Box>
                  <Typography variant="subtitle1" fontWeight={800}>
                    Identity Vibe
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Admin workspace
                  </Typography>
                </Box>
              </Stack>

              <Stack spacing={0.5}>
                <Typography variant="body2" color="text.secondary">
                  Secure operations, orders, and access control in one workspace.
                </Typography>
                <Button
                  variant="contained"
                  size="small"
                  sx={{ alignSelf: 'flex-start' }}
                >
                  Workspace ready
                </Button>
              </Stack>
            </Stack>
          )}
        </Paper>
      </Tooltip>

      {!collapsed ? (
        <Typography
          variant="overline"
          color="text.secondary"
          sx={{ px: 1, mt: 3, mb: 1.5, fontWeight: 700, letterSpacing: '0.08em' }}
        >
          Workspace
        </Typography>
      ) : null}

      <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', pr: collapsed ? 0 : 0.5, mt: collapsed ? 2 : 0 }}>
        <List disablePadding sx={{ display: 'grid', gap: 0.5 }}>
          {primaryNavItems.map((item) => {
            const isActive = item.href === '/orders'
              ? pathname === '/orders' || pathname.startsWith('/orders/')
              : pathname === item.href;

            return (
              <ListItem key={item.href} disablePadding>
                <Tooltip title={item.title} placement="right" disableHoverListener={!collapsed}>
                  <ListItemButton
                    component={Link}
                    href={item.href}
                    aria-current={isActive ? 'page' : undefined}
                    aria-label={collapsed ? item.title : undefined}
                    onMouseEnter={() => prefetchRoute(item.href)}
                    onFocus={() => prefetchRoute(item.href)}
                    onClick={() => {
                      if (!isActive) {
                        startNavigation();
                      }

                      onNavigate?.();
                    }}
                    disabled={isNavigating && !isActive}
                    sx={{
                      minHeight: 44,
                      px: collapsed ? 1.25 : 1.5,
                      py: 0.75,
                      justifyContent: collapsed ? 'center' : 'flex-start',
                      color: isActive ? 'primary.main' : 'text.secondary',
                      bgcolor: isActive ? 'action.selected' : 'transparent',
                      boxShadow: 'none',
                      '&:hover': {
                        bgcolor: isActive
                          ? (theme) => alpha(theme.palette.primary.main, 0.24)
                          : 'action.hover',
                      },
                    }}
                  >
                    <ListItemIcon
                      sx={{
                        minWidth: collapsed ? 0 : 34,
                        mr: collapsed ? 0 : 0.25,
                        color: 'inherit',
                        justifyContent: 'center',
                      }}
                    >
                      <Icon icon={item.icon} width={20} />
                    </ListItemIcon>
                    {!collapsed ? (
                      <ListItemText
                        primary={item.title}
                        primaryTypographyProps={{
                          fontSize: 14,
                          fontWeight: isActive ? 700 : 500,
                        }}
                      />
                    ) : null}
                  </ListItemButton>
                </Tooltip>
              </ListItem>
            );
          })}
        </List>
      </Box>

      <Tooltip title="Identity team" placement="right" disableHoverListener={!collapsed}>
        <Paper
          elevation={0}
          sx={{
            mt: 2,
            p: collapsed ? 1.25 : 2,
            border: 1,
            borderColor: 'divider',
          }}
        >
          <Stack
            direction="row"
            spacing={collapsed ? 0 : 1.5}
            alignItems="center"
            justifyContent={collapsed ? 'center' : 'flex-start'}
          >
            <Avatar sx={{ width: 40, height: 40, bgcolor: 'primary.main' }}>
              I
            </Avatar>
            {!collapsed ? (
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="subtitle2" noWrap>
                  Identity team
                </Typography>
                <Typography variant="body2" color="text.secondary" noWrap>
                  Daily operations dashboard
                </Typography>
              </Box>
            ) : null}
          </Stack>
        </Paper>
      </Tooltip>
    </Stack>
  );
}

type AppSidebarProps = {
  mobileOpen: boolean;
  onCloseMobile: () => void;
  desktopExpanded: boolean;
};

export function AppSidebar({
  mobileOpen,
  onCloseMobile,
  desktopExpanded,
}: AppSidebarProps) {
  return (
    <>
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onCloseMobile}
        ModalProps={{ keepMounted: true }}
        sx={{
          display: { xs: 'block', lg: 'none' },
          '& .MuiDrawer-paper': {
            width: drawerWidth,
            borderRight: 'none',
            boxSizing: 'border-box',
          },
        }}
      >
        <SidebarContent onNavigate={onCloseMobile} />
      </Drawer>

      <Drawer
        variant="permanent"
        open
        sx={{
          display: { xs: 'none', lg: 'block' },
          width: desktopExpanded ? drawerWidth : collapsedDrawerWidth,
          flexShrink: 0,
          transition: (theme) => theme.transitions.create('width', {
            duration: theme.transitions.duration.shorter,
          }),
          '& .MuiDrawer-paper': {
            width: desktopExpanded ? drawerWidth : collapsedDrawerWidth,
            position: 'relative',
            borderRight: '1px solid',
            borderColor: 'divider',
            boxSizing: 'border-box',
            overflowX: 'visible',
            transition: (theme) => theme.transitions.create('width', {
              duration: theme.transitions.duration.shorter,
            }),
          },
        }}
      >
        <SidebarContent collapsed={!desktopExpanded} />
      </Drawer>
    </>
  );
}

export const appSidebarWidth = drawerWidth;
export const appSidebarCollapsedWidth = collapsedDrawerWidth;
