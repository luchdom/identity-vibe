'use client';

import { useEffect, useMemo, useState } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import Button from '@mui/material/Button';
import Checkbox from '@mui/material/Checkbox';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormGroup from '@mui/material/FormGroup';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';

import type { AdminRoleOption, AdminUserDetail } from '@/lib/admin-users-api';

const ADMIN_MANAGEMENT_SCOPES = new Set(['admin.users', 'admin.full']);

const userRolesSchema = z.object({
  roleNames: z.array(z.string()),
});

type UserRolesFormValues = z.infer<typeof userRolesSchema>;

type UserRolesDialogProps = {
  user: AdminUserDetail | null;
  availableRoles: AdminRoleOption[];
  currentUserId?: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (roleNames: string[]) => Promise<void>;
};

export function UserRolesDialog({
  user,
  availableRoles,
  currentUserId,
  open,
  onOpenChange,
  onSave,
}: UserRolesDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { isDirty },
  } = useForm<UserRolesFormValues>({
    resolver: zodResolver(userRolesSchema),
    mode: 'onChange',
    defaultValues: {
      roleNames: [],
    },
  });

  useEffect(() => {
    reset({
      roleNames: user?.roles ?? [],
    });
  }, [reset, user]);

  const selectedRoles = watch('roleNames');

  const roleToScopes = useMemo(() => {
    const map = new Map<string, string[]>();

    for (const role of availableRoles) {
      map.set(role.name, role.assignedScopes);
    }

    return map;
  }, [availableRoles]);

  const selectedScopes = useMemo(() => {
    const scopes = new Set<string>();

    for (const roleName of selectedRoles) {
      for (const scope of roleToScopes.get(roleName) ?? []) {
        scopes.add(scope);
      }
    }

    return scopes;
  }, [roleToScopes, selectedRoles]);

  const wouldLoseSelfAdminManagement = useMemo(() => {
    if (!user || !currentUserId || user.id !== currentUserId) {
      return false;
    }

    for (const requiredScope of ADMIN_MANAGEMENT_SCOPES) {
      if (selectedScopes.has(requiredScope)) {
        return false;
      }
    }

    return true;
  }, [currentUserId, selectedScopes, user]);

  const handleClose = () => {
    if (isSubmitting) {
      return;
    }

    onOpenChange(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    setIsSubmitting(true);

    try {
      await onSave(values.roleNames);
      onOpenChange(false);
    } finally {
      setIsSubmitting(false);
    }
  });

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      fullWidth
      maxWidth="sm"
      PaperProps={{ sx: { borderRadius: 3 } }}
    >
      <DialogTitle>Edit roles</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            Assign active roles. Inactive roles stay visible but cannot be changed.
          </Typography>

          <Controller
            control={control}
            name="roleNames"
            render={({ field }) => (
              <FormGroup
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  border: (theme) => `1px solid ${theme.palette.divider}`,
                  maxHeight: 280,
                  overflowY: 'auto',
                  gap: 1,
                }}
              >
                {availableRoles.map((role) => {
                  const checked = field.value.includes(role.name);

                  return (
                    <FormControlLabel
                      key={role.id}
                      control={(
                        <Checkbox
                          checked={checked}
                          disabled={!role.isActive || isSubmitting}
                          onChange={(_, nextChecked) => {
                            const nextValue = nextChecked
                              ? [...field.value, role.name]
                              : field.value.filter((roleName) => roleName !== role.name);

                            field.onChange(nextValue.sort());
                          }}
                        />
                      )}
                      label={(
                        <Stack spacing={0.25}>
                          <Stack direction="row" spacing={1} alignItems="center">
                            <Typography variant="body2" fontWeight={600}>
                              {role.name}
                            </Typography>
                            {!role.isActive ? (
                              <Typography variant="caption" color="error.main">
                                Inactive
                              </Typography>
                            ) : null}
                          </Stack>
                          {role.description ? (
                            <Typography variant="caption" color="text.secondary">
                              {role.description}
                            </Typography>
                          ) : null}
                        </Stack>
                      )}
                    />
                  );
                })}
              </FormGroup>
            )}
          />

          {wouldLoseSelfAdminManagement ? (
            <Typography variant="caption" color="error.main">
              You cannot remove your own admin management access.
            </Typography>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={() => void onSubmit()}
          disabled={isSubmitting || !isDirty || wouldLoseSelfAdminManagement}
          data-testid="edit-user-roles-submit"
        >
          {isSubmitting ? 'Saving...' : 'Save roles'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

