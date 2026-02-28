'use client';

import { useEffect, useMemo, useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import type { AdminUserDetail, UpdateUserPayload } from '@/lib/admin-users-api';

interface EditUserDialogProps {
  user: AdminUserDetail | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (payload: UpdateUserPayload) => Promise<void>;
}

export function EditUserDialog({ user, open, onOpenChange, onSave }: EditUserDialogProps) {
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    setEmail(user?.email ?? '');
    setPhoneNumber(user?.phoneNumber ?? '');
  }, [user]);

  const canSubmit = useMemo(() => {
    if (!user) {
      return false;
    }

    const hasChanges = email.trim() !== user.email || phoneNumber.trim() !== (user.phoneNumber ?? '');
    return hasChanges && email.trim().length > 0 && !isSubmitting;
  }, [email, isSubmitting, phoneNumber, user]);

  const handleSubmit = async () => {
    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSave({
        email: email.trim(),
        phoneNumber: phoneNumber.trim() || undefined,
      });
      onOpenChange(false);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit User</DialogTitle>
          <DialogDescription>
            Update core profile fields for this account.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="edit-user-email">Email</label>
            <input
              id="edit-user-email"
              data-testid="edit-user-email"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="edit-user-phone">Phone Number</label>
            <input
              id="edit-user-phone"
              data-testid="edit-user-phone"
              className="h-9 w-full rounded-md border px-3 text-sm"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSubmit} disabled={!canSubmit} data-testid="edit-user-submit">
            {isSubmitting ? 'Saving...' : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
