import { api } from './api';
import type {
  OrderDetails,
  OrderListResponse,
  OrdersQueryParams,
  CreateOrderRequest,
  UpdateOrderRequest,
  UpdateOrderStatusRequest,
  AddTrackingNumberRequest,
  OrderStatusHistory,
} from '@/types/orders';

export class OrdersApi {
  private static readonly BASE_PATH = '/orders';

  /**
   * Get orders for the current user with pagination and filtering
   */
  static async getOrders(params?: OrdersQueryParams): Promise<OrderListResponse> {
    const searchParams = new URLSearchParams();
    
    if (params?.page) searchParams.set('page', params.page.toString());
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString());
    if (params?.status !== undefined) searchParams.set('status', params.status.toString());

    const url = `${this.BASE_PATH}?${searchParams.toString()}`;
    const response = await api.get<OrderListResponse>(url);
    return response.data;
  }

  /**
   * Get all orders (admin only) with pagination and filtering
   */
  static async getAllOrders(params?: OrdersQueryParams): Promise<OrderListResponse> {
    const searchParams = new URLSearchParams();
    
    if (params?.page) searchParams.set('page', params.page.toString());
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString());
    if (params?.status !== undefined) searchParams.set('status', params.status.toString());

    const url = `${this.BASE_PATH}/all?${searchParams.toString()}`;
    const response = await api.get<OrderListResponse>(url);
    return response.data;
  }

  /**
   * Get specific order by ID
   */
  static async getOrderById(id: number): Promise<OrderDetails> {
    const response = await api.get<OrderDetails>(`${this.BASE_PATH}/${id}`);
    return response.data;
  }

  /**
   * Get order status and history
   */
  static async getOrderStatus(id: number): Promise<{
    orderId: number;
    currentStatus: number;
    statusDisplay: string;
    statusHistory: OrderStatusHistory[];
  }> {
    const response = await api.get(`${this.BASE_PATH}/${id}/status`);
    return response.data;
  }

  /**
   * Create a new order
   */
  static async createOrder(order: CreateOrderRequest): Promise<{ order: OrderDetails }> {
    const response = await api.post<{ order: OrderDetails }>(`${this.BASE_PATH}`, order);
    return response.data;
  }

  /**
   * Update an existing order
   */
  static async updateOrder(id: number, order: UpdateOrderRequest): Promise<{ order: OrderDetails }> {
    const response = await api.put<{ order: OrderDetails }>(`${this.BASE_PATH}/${id}`, order);
    return response.data;
  }

  /**
   * Cancel an order
   */
  static async cancelOrder(id: number, reason?: string): Promise<{ order: OrderDetails }> {
    const response = await api.post<{ order: OrderDetails }>(`${this.BASE_PATH}/${id}/cancel`, {
      reason,
    });
    return response.data;
  }

  /**
   * Update order status (admin only)
   */
  static async updateOrderStatus(
    id: number,
    statusUpdate: UpdateOrderStatusRequest
  ): Promise<{ order: OrderDetails }> {
    const response = await api.post<{ order: OrderDetails }>(`${this.BASE_PATH}/${id}/status`, statusUpdate);
    return response.data;
  }

  /**
   * Add tracking number to an order (admin only)
   */
  static async addTrackingNumber(
    id: number,
    tracking: AddTrackingNumberRequest
  ): Promise<{ order: OrderDetails }> {
    const response = await api.post<{ order: OrderDetails }>(`${this.BASE_PATH}/${id}/tracking`, tracking);
    return response.data;
  }

  /**
   * Helper method to check if user has admin permissions
   */
  static isAdmin(): boolean {
    try {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      return user.roles?.includes('Admin') || user.roles?.includes('ServiceIdentity') || false;
    } catch {
      return false;
    }
  }
}

// Export convenience methods
export const ordersApi = OrdersApi;