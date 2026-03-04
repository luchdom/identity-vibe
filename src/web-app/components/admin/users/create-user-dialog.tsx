'use client';

import { useMemo, useState } from 'react';

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
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { Icon } from '@iconify/react';

import type { AdminRoleOption, CreateUserPayload } from '@/lib/admin-users-api';

const createUserSchema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
  phoneNumber: z.string().trim().optional(),
  isActive: z.boolean(),
  roleNames: z.array(z.string()).min(1, 'Select at least one role'),
});

type CreateUserFormValues = z.infer<typeof createUserSchema>;

type CreateUserDialogProps = {
  availableRoles: AdminRoleOption[];
  onCreate: (payload: CreateUserPayload) => Promise<void>;
};

const defaultValues: CreateUserFormValues = {
  email: '',
  password: '',
  phoneNumber: '',
  isActive: true,
  roleNames: [],
};

export function CreateUserDialog({
  availableRoles,
  onCreate,
}: CreateUserDialogProps) {
  const [open, setOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const selectableRoles = useMemo(
    () => availableRoles.filter((role) => role.isActive),
    [availableRoles]
  );

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isValid },
  } = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserSchema),
    mode: 'onChange',
    defaultValues,
  });

  const handleClose = () => {
    if (isSubmitting) {
      return;
    }

    reset(defaultValues);
    setOpen(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    setIsSubmitting(true);

    try {
      await onCreate({
        email: values.email.trim(),
        password: values.password,
        phoneNumber: values.phoneNumber?.trim() || undefined,
        isActive: values.isActive,
        roleNames: values.roleNames,
      });
      reset(defaultValues);
      setOpen(false);
    } finally {
      setIsSubmitting(false);
    }
  });

  return (
    <>
      <Button
        size="small"
        variant="contained"
        color="inherit"
        startIcon={<Icon icon="mingcute:add-line" width={18} />}
        data-testid="new-user-button"
        onClick={() => setOpen(true)}
        sx={{ height: 36, borderRadius: 1, px: 1.5 }}
      >
        New user
      </Button>

      <Dialog
        open={open}
        onClose={handleClose}
        fullWidth
        maxWidth="sm"
        PaperProps={{
          sx: {
            borderRadius: 3,
          },
        }}
      >
        <DialogTitle>Create user</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            <Controller
              control={control}
              name="email"
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Email"
                  type="email"
                  fullWidth
                  autoFocus
                  disabled={isSubmitting}
                  error={Boolean(errors.email)}
                  helperText={errors.email?.message}
                  inputProps={{ 'data-testid': 'create-user-email' }}
                />
              )}
            />

            <Controller
              control={control}
              name="password"
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Password"
                  type="password"
                  fullWidth
                  disabled={isSubmitting}
                  error={Boolean(errors.password)}
                  helperText={errors.password?.message}
                  inputProps={{ 'data-testid': 'create-user-password' }}
                />
              )}
            />

            <Controller
              control={control}
              name="phoneNumber"
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Phone number"
                  fullWidth
                  disabled={isSubmitting}
                  error={Boolean(errors.phoneNumber)}
                  helperText={errors.phoneNumber?.message}
                  inputProps={{ 'data-testid': 'create-user-phone' }}
                />
              )}
            />

            <Controller
              control={control}
              name="isActive"
              render={({ field }) => (
                <FormControlLabel
                  control={(
                    <Switch
                      checked={field.value}
                      onChange={(_, checked) => field.onChange(checked)}
                      disabled={isSubmitting}
                    />
                  )}
                  label="Create as active user"
                />
              )}
            />

            <Controller
              control={control}
              name="roleNames"
              render={({ field }) => (
                <Stack spacing={1.25}>
                  <Typography variant="subtitle2">Roles</Typography>
                  <FormGroup
                    sx={{
                      p: 1.5,
                      borderRadius: 2,
                      border: (theme) => `1px solid ${theme.palette.divider}`,
                      maxHeight: 240,
                      overflowY: 'auto',
                      gap: 1,
                    }}
                  >
                    {selectableRoles.map((role) => {
                      const checked = field.value.includes(role.name);

                      return (
                        <FormControlLabel
                          key={role.id}
                          control={(
                            <Checkbox
                              checked={checked}
                              onChange={(_, nextChecked) => {
                                const nextValue = nextChecked
                                  ? [...field.value, role.name]
                                  : field.value.filter((roleName) => roleName !== role.name);

                                field.onChange(nextValue.sort());
                              }}
                              disabled={isSubmitting}
                            />
                          )}
                          label={(
                            <Stack spacing={0.25}>
                              <Typography variant="body2" fontWeight={600}>
                                {role.name}
                              </Typography>
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

                    {selectableRoles.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">
                        No active roles are available yet.
                      </Typography>
                    ) : null}
                  </FormGroup>
                  {errors.roleNames ? (
                    <Typography variant="caption" color="error.main">
                      {errors.roleNames.message}
                    </Typography>
                  ) : null}
                </Stack>
              )}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={() => void onSubmit()}
            disabled={isSubmitting || !isValid}
            data-testid="create-user-submit"
          >
            {isSubmitting ? 'Creating...' : 'Create user'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
