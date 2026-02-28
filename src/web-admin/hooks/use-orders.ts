'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { ordersApi } from '@/lib/orders-api';
import { canViewAllOrders } from '@/lib/authz';
import { useAuth } from '@/contexts/auth-context';
import type { OrderListResponse, OrdersQueryParams, OrderStatus } from '@/types/orders';

interface UseOrdersResult {
  data: OrderListResponse | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export function useOrders(params?: OrdersQueryParams): UseOrdersResult {
  const { isAuthenticated, isLoading: authLoading, scopes } = useAuth();
  const [data, setData] = useState<OrderListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchOrders = useCallback(async () => {
    // Don't fetch if auth is still loading or user is not authenticated
    if (authLoading || !isAuthenticated) {
      return;
    }

    try {
      setLoading(true);
      setError(null);
      
      // Check if user is admin to determine which endpoint to use
      const isAdmin = canViewAllOrders(scopes);
      const response = isAdmin 
        ? await ordersApi.getAllOrders(params)
        : await ordersApi.getOrders(params);
      
      setData(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch orders');
      console.error('Error fetching orders:', err);
    } finally {
      setLoading(false);
    }
  }, [params, authLoading, isAuthenticated, scopes]);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  return {
    data,
    loading,
    error,
    refetch: fetchOrders,
  };
}

// Hook for fetching orders with status filtering from URL
export function useOrdersWithStatus(status?: OrderStatus, page?: number, pageSize?: number) {
  const params = useMemo(() => {
    const result: OrdersQueryParams = {};

    if (status !== undefined) result.status = status;
    if (page !== undefined) result.page = page;
    if (pageSize !== undefined) result.pageSize = pageSize;

    return result;
  }, [status, page, pageSize]);

  return useOrders(params);
}
