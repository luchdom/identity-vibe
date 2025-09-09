import { api } from './api';
import type {
  OrderListResponse,
  OrderResponse,
  OrderQuery,
  UpdateOrderStatusRequest,
  CancelOrderRequest,
} from '../types/orders';

export class OrdersAPI {
  private static readonly BASE_PATH = '/orders';

  /**
   * Get user's orders with optional filtering and pagination
   */
  static async getUserOrders(query: OrderQuery = {}): Promise<OrderListResponse> {
    const params = new URLSearchParams();
    
    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.status !== undefined) params.append('status', query.status.toString());
    if (query.search) params.append('search', query.search);
    if (query.sortBy) params.append('sortBy', query.sortBy);
    if (query.sortOrder) params.append('sortOrder', query.sortOrder);

    const response = await api.get(`${this.BASE_PATH}?${params.toString()}`);
    return response.data;
  }

  /**
   * Get all orders (admin only)
   */
  static async getAllOrders(query: OrderQuery = {}): Promise<OrderListResponse> {
    const params = new URLSearchParams();
    
    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.status !== undefined) params.append('status', query.status.toString());
    if (query.search) params.append('search', query.search);
    if (query.sortBy) params.append('sortBy', query.sortBy);
    if (query.sortOrder) params.append('sortOrder', query.sortOrder);

    const response = await api.get(`${this.BASE_PATH}/all?${params.toString()}`);
    return response.data;
  }

  /**
   * Get order details by ID
   */
  static async getOrderById(orderId: number): Promise<OrderResponse> {
    const response = await api.get(`${this.BASE_PATH}/${orderId}`);
    return response.data;
  }

  /**
   * Cancel an order
   */
  static async cancelOrder(orderId: number, cancelRequest: CancelOrderRequest = {}): Promise<void> {
    await api.post(`${this.BASE_PATH}/${orderId}/cancel`, cancelRequest);
  }

  /**
   * Get order status history
   */
  static async getOrderStatusHistory(orderId: number) {
    const response = await api.get(`${this.BASE_PATH}/${orderId}/status`);
    return response.data;
  }

  /**
   * Update order status (admin only)
   */
  static async updateOrderStatus(orderId: number, updateRequest: UpdateOrderStatusRequest): Promise<void> {
    await api.post(`${this.BASE_PATH}/${orderId}/status`, updateRequest);
  }

  /**
   * Get order statistics for dashboard (if needed)
   */
  static async getOrderStatistics() {
    try {
      const response = await api.get(`${this.BASE_PATH}/statistics`);
      return response.data;
    } catch (error) {
      // Return default stats if endpoint doesn't exist
      return {
        totalOrders: 0,
        pendingOrders: 0,
        processedOrders: 0,
        revenue: 0,
      };
    }
  }
}

export default OrdersAPI;