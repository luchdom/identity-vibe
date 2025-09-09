import { useState, useEffect } from 'react';

/**
 * Hook that debounces a value by delaying updates until after wait milliseconds have elapsed
 * since the last time the debounced function was invoked.
 */
export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    // Cleanup function that cancels the timeout if value changes before delay
    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}