'use client';

import type { Dispatch, SetStateAction } from 'react';

import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import Divider from '@mui/material/Divider';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemText from '@mui/material/ListItemText';
import OutlinedInput from '@mui/material/OutlinedInput';
import Stack from '@mui/material/Stack';
import { alpha } from '@mui/material/styles';
import type { SxProps, Theme } from '@mui/material/styles';
import Typography from '@mui/material/Typography';

import { Icon } from '@iconify/react';

import { AppLabel } from '@/components/common/app-label';
import { SurfacePanel } from '@/components/common/surface-panel';
import type { AdminRoleSummary } from '@/lib/admin-roles-api';

type RolesListProps = {
  roles: AdminRoleSummary[];
  selectedRoleId: string | null;
  isLoading?: boolean;
  onSelectRole: (roleId: string) => void;
  search: string;
  onSearchChange: Dispatch<SetStateAction<string>>;
  panelSx?: SxProps<Theme>;
};

export function RolesList({
  roles,
  selectedRoleId,
  isLoading = false,
  onSelectRole,
  search,
  onSearchChange,
  panelSx,
}: RolesListProps) {
  return (
    <SurfacePanel
      sx={[
        {
          display: 'flex',
          flexDirection: 'column',
          minHeight: 0,
        },
        ...(Array.isArray(panelSx) ? panelSx : [panelSx]),
      ]}
    >
      <Stack spacing={1.5} sx={{ p: 2 }}>
        <Typography variant="subtitle1" fontWeight={700}>
          Roles
        </Typography>

        <OutlinedInput
          size="small"
          value={search}
          placeholder="Search roles..."
          onChange={(event) => onSearchChange(event.target.value)}
          startAdornment={<Icon icon="eva:search-fill" width={16} />}
          inputProps={{ 'data-testid': 'roles-search' }}
          sx={{
            '&.MuiOutlinedInput-root': {
              height: 36,
              borderRadius: 1,
              fontSize: 14,
            },
            '& .MuiInputBase-input': {
              py: 0,
              px: 1.25,
            },
            '& .iconify': {
              color: 'text.secondary',
              mr: 1,
            },
          }}
        />
      </Stack>

      <Divider />

      {isLoading ? (
        <Stack spacing={1.5} alignItems="center" justifyContent="center" sx={{ py: 8, px: 2, flex: 1 }}>
          <CircularProgress size={24} />
          <Typography variant="body2" color="text.secondary">
            Loading roles...
          </Typography>
        </Stack>
      ) : roles.length === 0 ? (
        <Box sx={{ p: 2, flex: 1 }}>
          <Typography variant="body2" color="text.secondary">
            No roles found.
          </Typography>
        </Box>
      ) : (
        <List disablePadding sx={{ p: 1, display: 'grid', gap: 0.75, overflowY: 'auto', minHeight: 0 }}>
          {roles.map((role) => {
            const isSelected = role.id === selectedRoleId;
            return (
              <ListItemButton
                key={role.id}
                selected={isSelected}
                onClick={() => onSelectRole(role.id)}
                data-testid={`role-item-${role.name}`}
                sx={{
                  position: 'relative',
                  overflow: 'hidden',
                  borderRadius: 1,
                  border: '1px solid',
                  borderColor: isSelected ? 'primary.main' : 'divider',
                  alignItems: 'flex-start',
                  px: 1.5,
                  py: 1.25,
                  bgcolor: 'background.paper',
                  transition: (theme) => theme.transitions.create(
                    ['background-color', 'border-color', 'box-shadow'],
                    { duration: theme.transitions.duration.shorter }
                  ),
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    top: 8,
                    bottom: 8,
                    left: 0,
                    width: 3,
                    borderRadius: '0 999px 999px 0',
                    bgcolor: 'primary.main',
                    opacity: isSelected ? 1 : 0,
                    transition: (theme) => theme.transitions.create('opacity', {
                      duration: theme.transitions.duration.shorter,
                    }),
                  },
                  '&:hover': {
                    bgcolor: (theme) => theme.palette.action.hover,
                    borderColor: isSelected ? 'primary.main' : 'divider',
                  },
                  '&.Mui-selected': {
                    bgcolor: (theme) => alpha(theme.palette.primary.main, 0.08),
                    boxShadow: (theme) => `inset 0 0 0 1px ${theme.palette.primary.main}`,
                    '&:hover': {
                      bgcolor: (theme) => alpha(theme.palette.primary.main, 0.12),
                    },
                  },
                }}
              >
                <ListItemText
                  primary={(
                    <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
                      <Typography variant="body2" fontWeight={700}>
                        {role.name}
                      </Typography>
                      <AppLabel color={role.isActive ? 'success' : 'warning'}>
                        {role.isActive ? 'Active' : 'Disabled'}
                      </AppLabel>
                    </Stack>
                  )}
                  secondary={(
                    <Stack spacing={0.75} sx={{ mt: 0.75 }}>
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{
                          display: '-webkit-box',
                          overflow: 'hidden',
                          WebkitLineClamp: 2,
                          WebkitBoxOrient: 'vertical',
                        }}
                      >
                        {role.description || 'No description'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {role.assignedScopeCount} scope(s)
                      </Typography>
                    </Stack>
                  )}
                  secondaryTypographyProps={{ component: 'div' }}
                />
              </ListItemButton>
            );
          })}
        </List>
      )}
    </SurfacePanel>
  );
}
