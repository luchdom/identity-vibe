'use client';

import { useEffect, useState } from 'react';

export function useDelayedFlag(active: boolean, delayMs = 1200): boolean {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (!active) {
      setVisible(false);
      return;
    }

    const timeout = setTimeout(() => {
      setVisible(true);
    }, delayMs);

    return () => {
      clearTimeout(timeout);
    };
  }, [active, delayMs]);

  return visible;
}

