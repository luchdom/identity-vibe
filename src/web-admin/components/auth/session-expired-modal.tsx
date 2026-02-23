'use client';

import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { AlertCircle } from 'lucide-react';

interface SessionExpiredModalProps {
  open: boolean;
  onConfirm: () => void;
}

export function SessionExpiredModal({ open, onConfirm }: SessionExpiredModalProps) {
  return (
    <Dialog open={open} onOpenChange={() => {}}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-yellow-600" />
            Session Expired
          </DialogTitle>
          <DialogDescription>
            Your session has expired or you need to sign in again.
            Click continue to go to the login page.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button onClick={onConfirm}>
            Continue to Login
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}