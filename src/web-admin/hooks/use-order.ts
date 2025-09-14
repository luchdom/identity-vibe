'use client';

import { useState, useEffect, useCallback } from 'react';
import { ordersApi } from '@/lib/orders-api';
import { useAuth } from '@/contexts/auth-context';
import type { OrderDetails } from '@/types/orders';

interface UseOrderResult {
  data: OrderDetails | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export function useOrder(orderId: number): UseOrderResult {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const [data, setData] = useState<OrderDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchOrder = useCallback(async () => {
    if (!orderId) return;
    
    // Don't fetch if auth is still loading or user is not authenticated
    if (authLoading || !isAuthenticated) {
      return;
    }
    
    try {
      setLoading(true);
      setError(null);
      
      const response = await ordersApi.getOrderById(orderId);
      setData(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch order');
      console.error('Error fetching order:', err);
    } finally {
      setLoading(false);
    }
  }, [orderId, authLoading, isAuthenticated]);

  useEffect(() => {
    fetchOrder();
  }, [fetchOrder]);

  return {
    data,
    loading,
    error,
    refetch: fetchOrder,
  };
}