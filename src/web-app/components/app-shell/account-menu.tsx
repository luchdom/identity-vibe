'use client';

import { useState } from 'react';
import Link from 'next/link';

import Avatar from '@mui/material/Avatar';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { Icon } from '@iconify/react';

import { ThemeToggleButton } from './theme-toggle-button';
import { useAuth } from '@/contexts/auth-context';

type AccountMenuProps = {
  userName?: string;
  userEmail?: string;
};

export function AccountMenu({
  userName = 'Migration Preview',
  userEmail = 'web-app@local.test',
}: AccountMenuProps) {
  const { user, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const isOpen = Boolean(anchorEl);
  const displayName = [
    user?.firstName?.trim(),
    user?.lastName?.trim(),
  ]
    .filter(Boolean)
    .join(' ') || user?.email || userName;
  const displayEmail = user?.email || userEmail;

  return (
    <>
      <IconButton
        color="inherit"
        aria-label="Open account menu"
        onClick={(event) => setAnchorEl(event.currentTarget)}
      >
        <Avatar
          sx={{
            width: 36,
            height: 36,
            bgcolor: 'primary.main',
            color: 'primary.contrastText',
            fontWeight: 700,
          }}
        >
          {displayName.charAt(0).toUpperCase()}
        </Avatar>
      </IconButton>

      <Menu
        anchorEl={anchorEl}
        open={isOpen}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: {
            sx: {
              width: 300,
              p: 0.5,
              mt: 1,
            },
          },
        }}
      >
        <Stack spacing={0.25} sx={{ px: 1.5, py: 1.25 }}>
          <Typography variant="subtitle2">{displayName}</Typography>
          <Typography variant="body2" color="text.secondary">
            {displayEmail}
          </Typography>
        </Stack>

        <Divider sx={{ my: 0.5 }} />

        <MenuItem
          component={Link}
          href="/profile"
          onClick={() => setAnchorEl(null)}
        >
          <ListItemIcon>
            <Icon icon="solar:user-id-bold-duotone" width={20} />
          </ListItemIcon>
          <ListItemText
            primary="Profile"
            secondary="Read-only account page scaffold"
          />
        </MenuItem>

        <MenuItem disableRipple>
          <ListItemIcon>
            <Icon icon="solar:pallete-2-bold-duotone" width={20} />
          </ListItemIcon>
          <ListItemText
            primary="Theme"
            secondary="Light and dark mode ready"
          />
          <ThemeToggleButton />
        </MenuItem>

        <MenuItem
          onClick={() => {
            setAnchorEl(null);
            void logout();
          }}
        >
          <ListItemIcon>
            <Icon icon="solar:logout-3-bold-duotone" width={20} />
          </ListItemIcon>
          <ListItemText
            primary="Logout"
            secondary="End the current session"
          />
        </MenuItem>
      </Menu>
    </>
  );
}
