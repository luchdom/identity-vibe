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

const resetPasswordSchema = z.object({
  newPassword: z.string().min(1, 'New password is required'),
  confirmPassword: z.string().min(1, 'Confirm the new password'),
}).refine((value) => value.newPassword === value.confirmPassword, {
  path: ['confirmPassword'],
  message: 'Passwords do not match',
});

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

type ResetPasswordDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onResetPassword: (newPassword: string) => Promise<void>;
};

const defaultValues: ResetPasswordFormValues = {
  newPassword: '',
  confirmPassword: '',
};

export function ResetPasswordDialog({
  open,
  onOpenChange,
  onResetPassword,
}: ResetPasswordDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isValid },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    mode: 'onChange',
    defaultValues,
  });

  useEffect(() => {
    if (!open) {
      reset(defaultValues);
    }
  }, [open, reset]);

  const handleClose = () => {
    if (isSubmitting) {
      return;
    }

    reset(defaultValues);
    onOpenChange(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    setIsSubmitting(true);

    try {
      await onResetPassword(values.newPassword);
      reset(defaultValues);
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
      <DialogTitle>Reset password</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          <TextField
            {...register('newPassword')}
            label="New password"
            type="password"
            fullWidth
            autoFocus
            disabled={isSubmitting}
            error={Boolean(errors.newPassword)}
            helperText={errors.newPassword?.message}
            inputProps={{ 'data-testid': 'reset-password-new' }}
          />

          <TextField
            {...register('confirmPassword')}
            label="Confirm password"
            type="password"
            fullWidth
            disabled={isSubmitting}
            error={Boolean(errors.confirmPassword)}
            helperText={errors.confirmPassword?.message}
            inputProps={{ 'data-testid': 'reset-password-confirm' }}
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
          disabled={isSubmitting || !isDirty || !isValid}
          data-testid="reset-password-submit"
        >
          {isSubmitting ? 'Resetting...' : 'Reset password'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

