'use client';

import { useMemo, useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

interface ResetPasswordDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onResetPassword: (newPassword: string) => Promise<void>;
}

export function ResetPasswordDialog({ open, onOpenChange, onResetPassword }: ResetPasswordDialogProps) {
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const canSubmit = useMemo(() => {
    return (
      !isSubmitting &&
      newPassword.length > 0 &&
      confirmPassword.length > 0 &&
      newPassword === confirmPassword
    );
  }, [confirmPassword, isSubmitting, newPassword]);

  const resetState = () => {
    setNewPassword('');
    setConfirmPassword('');
  };

  const handleSubmit = async () => {
    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onResetPassword(newPassword);
      resetState();
      onOpenChange(false);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reset Password</DialogTitle>
          <DialogDescription>
            This uses token-based reset semantics on the server.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="reset-password-new">New Password</label>
            <input
              id="reset-password-new"
              data-testid="reset-password-new"
              type="password"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="reset-password-confirm">Confirm Password</label>
            <input
              id="reset-password-confirm"
              data-testid="reset-password-confirm"
              type="password"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              disabled={isSubmitting}
            />
            {confirmPassword.length > 0 && confirmPassword !== newPassword ? (
              <p className="text-xs text-destructive">Passwords do not match.</p>
            ) : null}
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSubmit} disabled={!canSubmit} data-testid="reset-password-submit">
            {isSubmitting ? 'Resetting...' : 'Reset Password'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
