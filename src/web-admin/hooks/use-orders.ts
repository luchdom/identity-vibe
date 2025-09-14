'use client';

import { useState, useEffect, useCallback } from 'react';
import { ordersApi } from '@/lib/orders-api';
import { useAuth } from '@/contexts/auth-context';
import type { OrderListResponse, OrdersQueryParams, OrderStatus } from '@/types/orders';

interface UseOrdersResult {
  data: OrderListResponse | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export function useOrders(params?: OrdersQueryParams): UseOrdersResult {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
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
      const isAdmin = ordersApi.isAdmin();
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
  }, [params, authLoading, isAuthenticated]);

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
  const params: OrdersQueryParams = {};
  
  if (status !== undefined) params.status = status;
  if (page !== undefined) params.page = page;
  if (pageSize !== undefined) params.pageSize = pageSize;

  return useOrders(params);
}