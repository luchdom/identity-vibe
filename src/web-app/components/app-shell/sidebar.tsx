'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

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
import Typography from '@mui/material/Typography';
import { alpha } from '@mui/material/styles';

import { Icon } from '@iconify/react';

import { primaryNavItems } from './nav-config';

const drawerWidth = 296;

type SidebarContentProps = {
  onNavigate?: () => void;
};

function SidebarContent({ onNavigate }: SidebarContentProps) {
  const pathname = usePathname();

  return (
    <Stack
      sx={{
        height: '100%',
        px: 2,
        py: 2.5,
        bgcolor: 'background.paper',
      }}
    >
      <Paper
        elevation={0}
        sx={{
          p: 2,
          border: 1,
          borderColor: 'divider',
          background: (theme) => theme.palette.mode === 'light'
            ? 'linear-gradient(135deg, rgba(32, 101, 209, 0.08), rgba(0, 167, 111, 0.04))'
            : 'linear-gradient(135deg, rgba(32, 101, 209, 0.18), rgba(0, 167, 111, 0.08))',
        }}
      >
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
                boxShadow: '0 12px 24px rgba(32, 101, 209, 0.22)',
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
      </Paper>

      <Typography
        variant="overline"
        color="text.secondary"
        sx={{ px: 1, mt: 3, mb: 1.5, fontWeight: 700, letterSpacing: '0.08em' }}
      >
        Workspace
      </Typography>

      <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', pr: 0.5 }}>
        <List disablePadding sx={{ display: 'grid', gap: 0.5 }}>
          {primaryNavItems.map((item) => {
            const isActive = item.href === '/orders'
              ? pathname === '/orders' || pathname.startsWith('/orders/')
              : pathname === item.href;

            return (
              <ListItem key={item.href} disablePadding>
                <ListItemButton
                  component={Link}
                  href={item.href}
                  onClick={onNavigate}
                  sx={{
                    minHeight: 44,
                    px: 1.5,
                    py: 0.75,
                    color: isActive ? 'primary.main' : 'text.secondary',
                    bgcolor: isActive ? (theme) => alpha(theme.palette.primary.main, 0.08) : 'transparent',
                    boxShadow: 'none',
                    '&:hover': {
                      bgcolor: isActive
                        ? (theme) => alpha(theme.palette.primary.main, 0.14)
                        : 'action.hover',
                    },
                  }}
                >
                  <ListItemIcon
                    sx={{
                      minWidth: 34,
                      color: 'inherit',
                    }}
                  >
                    <Icon icon={item.icon} width={20} />
                  </ListItemIcon>
                  <ListItemText
                    primary={item.title}
                    primaryTypographyProps={{
                      fontSize: 14,
                      fontWeight: isActive ? 700 : 500,
                    }}
                  />
                </ListItemButton>
              </ListItem>
            );
          })}
        </List>
      </Box>

      <Paper
        elevation={0}
        sx={{
          mt: 2,
          p: 2,
          border: 1,
          borderColor: 'divider',
        }}
      >
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Avatar sx={{ width: 40, height: 40, bgcolor: 'primary.main' }}>
            I
          </Avatar>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="subtitle2" noWrap>
              Identity team
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap>
              Daily operations dashboard
            </Typography>
          </Box>
        </Stack>
      </Paper>
    </Stack>
  );
}

type AppSidebarProps = {
  mobileOpen: boolean;
  onCloseMobile: () => void;
};

export function AppSidebar({ mobileOpen, onCloseMobile }: AppSidebarProps) {
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
          width: drawerWidth,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: drawerWidth,
            borderRight: '1px solid',
            borderColor: 'divider',
            boxSizing: 'border-box',
          },
        }}
      >
        <SidebarContent />
      </Drawer>
    </>
  );
}

export const appSidebarWidth = drawerWidth;
