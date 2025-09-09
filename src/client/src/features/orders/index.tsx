import { useState, useEffect, useCallback } from 'react';
import { toast } from 'sonner';
import { OrdersAPI } from '@/lib/orders-api';
import { OrdersTable } from './components/OrdersTable';
import { OrdersFilterBar } from './components/OrdersFilterBar';
import { OrderDetailsModal } from './components/OrderDetailsModal';
import type { 
  OrderListResponse, 
  OrderResponse, 
  CancelOrderRequest,
  UpdateOrderStatusRequest,
  FilterState
} from '@/types/orders';
import { useAuth } from '@/contexts/AuthContext';
import { 
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

export function Orders() {
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin') ?? false;

  // Data state
  const [ordersData, setOrdersData] = useState<OrderListResponse | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<OrderResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingOrderDetails, setIsLoadingOrderDetails] = useState(false);

  // UI state
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);
  const [orderToCancel, setOrderToCancel] = useState<number | null>(null);
  const [cancelReason, setCancelReason] = useState('');

  // Filter state
  const [filters, setFilters] = useState<FilterState>({
    search: '',
    status: undefined,
    sortBy: 'createdAt',
    sortOrder: 'desc',
  });
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  // Load orders data
  const loadOrders = useCallback(async () => {
    try {
      setIsLoading(true);
      const query = {
        page: currentPage,
        pageSize,
        status: filters.status,
        search: filters.search || undefined,
        sortBy: filters.sortBy,
        sortOrder: filters.sortOrder,
      };

      const data = isAdmin 
        ? await OrdersAPI.getAllOrders(query)
        : await OrdersAPI.getUserOrders(query);
      
      setOrdersData(data);
    } catch (error) {
      console.error('Failed to load orders:', error);
      toast.error('Failed to load orders. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, filters, isAdmin]);

  // Load order details
  const loadOrderDetails = useCallback(async (orderId: number) => {
    try {
      setIsLoadingOrderDetails(true);
      const order = await OrdersAPI.getOrderById(orderId);
      setSelectedOrder(order);
    } catch (error) {
      console.error('Failed to load order details:', error);
      toast.error('Failed to load order details. Please try again.');
    } finally {
      setIsLoadingOrderDetails(false);
    }
  }, []);

  // Effects
  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  useEffect(() => {
    if (selectedOrderId) {
      loadOrderDetails(selectedOrderId);
    }
  }, [selectedOrderId, loadOrderDetails]);

  // Event handlers
  const handleFiltersChange = useCallback((newFilters: FilterState) => {
    setFilters(newFilters);
    setCurrentPage(1); // Reset to first page when filters change
  }, []);

  const handlePageChange = useCallback((page: number) => {
    setCurrentPage(page);
  }, []);

  const handleViewOrder = useCallback((orderId: number) => {
    setSelectedOrderId(orderId);
    setIsDetailsModalOpen(true);
  }, []);

  const handleCloseDetailsModal = useCallback(() => {
    setIsDetailsModalOpen(false);
    setSelectedOrderId(null);
    setSelectedOrder(null);
  }, []);

  const handleCancelOrderRequest = useCallback((orderId: number) => {
    setOrderToCancel(orderId);
    setIsCancelDialogOpen(true);
    setCancelReason('');
  }, []);

  const handleCancelOrderConfirm = useCallback(async () => {
    if (!orderToCancel) return;

    try {
      const cancelRequest: CancelOrderRequest = {
        reason: cancelReason.trim() || undefined,
      };

      await OrdersAPI.cancelOrder(orderToCancel, cancelRequest);
      toast.success('Order cancelled successfully');
      
      // Refresh the orders list
      await loadOrders();
      
      // If the cancelled order is currently viewed, refresh its details
      if (selectedOrderId === orderToCancel) {
        await loadOrderDetails(orderToCancel);
      }
      
      // Close dialog
      setIsCancelDialogOpen(false);
      setOrderToCancel(null);
      setCancelReason('');
    } catch (error) {
      console.error('Failed to cancel order:', error);
      toast.error('Failed to cancel order. Please try again.');
    }
  }, [orderToCancel, cancelReason, selectedOrderId, loadOrders, loadOrderDetails]);

  const handleUpdateOrderStatus = useCallback(async (orderId: number, statusUpdate: UpdateOrderStatusRequest) => {
    try {
      await OrdersAPI.updateOrderStatus(orderId, statusUpdate);
      toast.success('Order status updated successfully');
      
      // Refresh the orders list
      await loadOrders();
      
      // If the updated order is currently viewed, refresh its details
      if (selectedOrderId === orderId) {
        await loadOrderDetails(orderId);
      }
    } catch (error) {
      console.error('Failed to update order status:', error);
      toast.error('Failed to update order status. Please try again.');
    }
  }, [selectedOrderId, loadOrders, loadOrderDetails]);

  return (
    <>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Orders</h1>
            <p className="text-muted-foreground">
              {isAdmin ? 'Manage all orders in the system' : 'View your orders'}
            </p>
          </div>
        </div>

        <OrdersFilterBar
          filters={filters}
          onFiltersChange={handleFiltersChange}
          isLoading={isLoading}
          totalCount={ordersData?.totalCount}
        />

        <OrdersTable
          data={ordersData}
          isLoading={isLoading}
          onViewOrder={handleViewOrder}
          onCancelOrder={handleCancelOrderRequest}
          onPageChange={handlePageChange}
          currentPage={currentPage}
        />
      </div>

      {/* Order Details Modal */}
      <OrderDetailsModal
        orderId={selectedOrderId}
        order={selectedOrder}
        isLoading={isLoadingOrderDetails}
        isOpen={isDetailsModalOpen}
        onClose={handleCloseDetailsModal}
        onCancelOrder={handleCancelOrderRequest}
        onUpdateStatus={isAdmin ? handleUpdateOrderStatus : undefined}
        isAdmin={isAdmin}
      />

      {/* Cancel Order Confirmation Dialog */}
      <AlertDialog open={isCancelDialogOpen} onOpenChange={setIsCancelDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Cancel Order</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to cancel this order? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="cancel-reason">Reason for cancellation (optional)</Label>
              <Textarea
                id="cancel-reason"
                placeholder="Enter the reason for cancelling this order..."
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                rows={3}
              />
            </div>
          </div>

          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => {
              setIsCancelDialogOpen(false);
              setOrderToCancel(null);
              setCancelReason('');
            }}>
              Keep Order
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleCancelOrderConfirm}
              className="bg-destructive hover:bg-destructive/90"
            >
              Cancel Order
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}