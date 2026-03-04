'use client';

import { useState } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { Icon } from '@iconify/react';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

import { adminRolesApi } from '@/lib/admin-roles-api';

const createRoleSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, 'Role name is required')
    .max(64, 'Role name must be 64 characters or less')
    .regex(/^[A-Za-z0-9._-]+$/, 'Use letters, numbers, dot, dash, underscore'),
  description: z.string().trim().optional(),
});

type CreateRoleFormValues = z.infer<typeof createRoleSchema>;

type CreateRoleDialogProps = {
  onCreated: (roleId: string) => Promise<void> | void;
};

const defaultValues: CreateRoleFormValues = {
  name: '',
  description: '',
};

export function CreateRoleDialog({ onCreated }: CreateRoleDialogProps) {
  const [open, setOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isValid },
  } = useForm<CreateRoleFormValues>({
    resolver: zodResolver(createRoleSchema),
    mode: 'onChange',
    defaultValues,
  });

  const handleClose = () => {
    if (isSaving) {
      return;
    }

    reset(defaultValues);
    setSubmitError(null);
    setOpen(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    setSubmitError(null);
    setIsSaving(true);

    try {
      const created = await adminRolesApi.createRole({
        name: values.name.trim(),
        description: values.description?.trim() || undefined,
        isActive: true,
      });

      reset(defaultValues);
      setOpen(false);
      await onCreated(created.id);
    } catch (error: unknown) {
      const maybeAxios = error as { response?: { data?: { detail?: string } }; message?: string };
      setSubmitError(maybeAxios.response?.data?.detail || maybeAxios.message || 'Failed to create role');
    } finally {
      setIsSaving(false);
    }
  });

  return (
    <>
      <Button
        size="small"
        variant="contained"
        color="inherit"
        startIcon={<Icon icon="mingcute:add-line" width={18} />}
        onClick={() => setOpen(true)}
        data-testid="new-role-button"
        sx={{ height: 36, borderRadius: 1, px: 1.5 }}
      >
        New role
      </Button>

      <Dialog
        open={open}
        onClose={handleClose}
        fullWidth
        maxWidth="sm"
        PaperProps={{ sx: { borderRadius: 2 } }}
      >
        <DialogTitle>Create role</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            <TextField
              {...register('name')}
              label="Role name"
              placeholder="e.g. Auditor"
              fullWidth
              autoFocus
              disabled={isSaving}
              error={Boolean(errors.name)}
              helperText={errors.name?.message}
            />

            <TextField
              {...register('description')}
              label="Description"
              placeholder="Describe what this role is used for"
              fullWidth
              multiline
              minRows={3}
              disabled={isSaving}
              error={Boolean(errors.description)}
              helperText={errors.description?.message}
            />

            {submitError ? (
              <Typography variant="body2" color="error.main">
                {submitError}
              </Typography>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button size="small" onClick={handleClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button
            size="small"
            variant="contained"
            color="inherit"
            onClick={() => void onSubmit()}
            disabled={isSaving || !isValid}
          >
            {isSaving ? 'Creating...' : 'Create role'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
