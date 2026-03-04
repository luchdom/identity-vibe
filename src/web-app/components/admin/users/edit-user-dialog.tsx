'use client';

import { useEffect, useState } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

import type { AdminUserDetail, UpdateUserPayload } from '@/lib/admin-users-api';

const editUserSchema = z.object({
  email: z.string().email('Enter a valid email address'),
  phoneNumber: z.string().trim().optional(),
});

type EditUserFormValues = z.infer<typeof editUserSchema>;

type EditUserDialogProps = {
  user: AdminUserDetail | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (payload: UpdateUserPayload) => Promise<void>;
};

export function EditUserDialog({
  user,
  open,
  onOpenChange,
  onSave,
}: EditUserDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isValid },
  } = useForm<EditUserFormValues>({
    resolver: zodResolver(editUserSchema),
    mode: 'onChange',
    defaultValues: {
      email: '',
      phoneNumber: '',
    },
  });

  useEffect(() => {
    reset({
      email: user?.email ?? '',
      phoneNumber: user?.phoneNumber ?? '',
    });
  }, [reset, user]);

  const handleClose = () => {
    if (isSubmitting) {
      return;
    }

    onOpenChange(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (!user) {
      return;
    }

    setIsSubmitting(true);

    try {
      await onSave({
        email: values.email.trim(),
        phoneNumber: values.phoneNumber?.trim() || undefined,
      });
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
      <DialogTitle>Edit user</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          <TextField
            {...register('email')}
            label="Email"
            type="email"
            fullWidth
            autoFocus
            disabled={isSubmitting}
            error={Boolean(errors.email)}
            helperText={errors.email?.message}
            inputProps={{ 'data-testid': 'edit-user-email' }}
          />

          <TextField
            {...register('phoneNumber')}
            label="Phone number"
            fullWidth
            disabled={isSubmitting}
            error={Boolean(errors.phoneNumber)}
            helperText={errors.phoneNumber?.message}
            inputProps={{ 'data-testid': 'edit-user-phone' }}
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
          disabled={isSubmitting || !isDirty || !isValid || !user}
          data-testid="edit-user-submit"
        >
          {isSubmitting ? 'Saving...' : 'Save changes'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
