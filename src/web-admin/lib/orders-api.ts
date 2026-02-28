import { ordersApi as ordersHttp } from './api';
import { getStoredGrantedScopes, canViewAllOrders } from './authz';
import { OrderStatus } from '@/types/orders';
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

type ApiOrderItemResponse = {
  id: number;
  productSku?: string | null;
  productName: string;
  displayName?: string | null;
  productDescription?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

type ApiCustomerResponse = {
  fullName: string;
  email: string;
  phone?: string | null;
  address?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
};

type ApiShippingResponse = {
  address?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  shippingMethod?: string | null;
};

type ApiOrderStatusHistoryResponse = {
  id: number;
  toStatus: number;
  statusChange?: string | null;
  notes?: string | null;
  reason?: string | null;
  changedByUserId: string;
  changedByUserName?: string | null;
  createdAt: string;
};

type ApiOrderDetailsResponse = {
  id: number;
  orderNumber: string;
  status: number;
  statusDisplay: string;
  customer: ApiCustomerResponse;
  items: ApiOrderItemResponse[];
  subtotalAmount: number;
  taxAmount: number;
  shippingAmount: number;
  discountAmount: number;
  totalAmount: number;
  currency: string;
  trackingNumber?: string | null;
  paymentMethod?: string | null;
  paymentReference?: string | null;
  shipping?: ApiShippingResponse | null;
  specialInstructions?: string | null;
  createdAt: string;
  updatedAt: string;
  shippedAt?: string | null;
  deliveredAt?: string | null;
  cancelledAt?: string | null;
  itemCount: number;
  canBeCancelled: boolean;
  canBeModified: boolean;
  statusHistory: ApiOrderStatusHistoryResponse[];
};

function toFiniteNumber(value: unknown, fallback = 0): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function statusLabel(status: number): string {
  const label = (OrderStatus as unknown as Record<number, string>)[status];
  return typeof label === 'string' ? label : String(status);
}

function mapOrderDetails(raw: ApiOrderDetailsResponse): OrderDetails {
  return {
    id: raw.id,
    orderNumber: raw.orderNumber,
    status: raw.status as OrderStatus,
    statusDisplay: raw.statusDisplay,
    customer: {
      name: raw.customer?.fullName ?? '',
      email: raw.customer?.email ?? '',
      phone: raw.customer?.phone ?? undefined,
      address: raw.customer?.address ?? undefined,
      city: raw.customer?.city ?? undefined,
      state: raw.customer?.state ?? undefined,
      postalCode: raw.customer?.postalCode ?? undefined,
      country: raw.customer?.country ?? undefined,
    },
    items: (raw.items ?? []).map((item) => ({
      id: item.id,
      productName: item.productName ?? item.displayName ?? 'Unknown Product',
      sku: item.productSku ?? undefined,
      quantity: toFiniteNumber(item.quantity, 0),
      unitPrice: toFiniteNumber(item.unitPrice, 0),
      totalPrice: toFiniteNumber(item.lineTotal, 0),
      description: item.productDescription ?? undefined,
    })),
    subtotalAmount: toFiniteNumber(raw.subtotalAmount, 0),
    taxAmount: toFiniteNumber(raw.taxAmount, 0),
    shippingAmount: toFiniteNumber(raw.shippingAmount, 0),
    discountAmount: toFiniteNumber(raw.discountAmount, 0),
    totalAmount: toFiniteNumber(raw.totalAmount, 0),
    currency: raw.currency ?? 'USD',
    trackingNumber: raw.trackingNumber ?? undefined,
    paymentMethod: raw.paymentMethod ?? undefined,
    paymentReference: raw.paymentReference ?? undefined,
    shipping: raw.shipping
      ? {
          method: raw.shipping.shippingMethod?.trim() ? raw.shipping.shippingMethod : 'N/A',
          address: raw.shipping.address ?? '',
          city: raw.shipping.city ?? '',
          state: raw.shipping.state ?? '',
          postalCode: raw.shipping.postalCode ?? '',
          country: raw.shipping.country ?? '',
        }
      : undefined,
    specialInstructions: raw.specialInstructions ?? undefined,
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt,
    shippedAt: raw.shippedAt ?? undefined,
    deliveredAt: raw.deliveredAt ?? undefined,
    cancelledAt: raw.cancelledAt ?? undefined,
    itemCount: raw.itemCount,
    canBeCancelled: raw.canBeCancelled,
    canBeModified: raw.canBeModified,
    statusHistory: (raw.statusHistory ?? []).map((history) => ({
      id: history.id,
      status: history.toStatus as OrderStatus,
      statusDisplay: history.statusChange?.trim() || statusLabel(history.toStatus),
      notes: history.notes ?? history.reason ?? undefined,
      createdAt: history.createdAt,
      createdBy: history.changedByUserName?.trim() || history.changedByUserId || 'System',
    })),
  };
}

function mapOrderWrapper<T extends { order: ApiOrderDetailsResponse }>(raw: T): { order: OrderDetails } {
  return { order: mapOrderDetails(raw.order) };
}

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
    const response = await ordersHttp.get<OrderListResponse>(url);
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
    const response = await ordersHttp.get<OrderListResponse>(url);
    return response.data;
  }

  /**
   * Get specific order by ID
   */
  static async getOrderById(id: number): Promise<OrderDetails> {
    const response = await ordersHttp.get<ApiOrderDetailsResponse>(`${this.BASE_PATH}/${id}`);
    return mapOrderDetails(response.data);
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
    const response = await ordersHttp.get(`${this.BASE_PATH}/${id}/status`);
    return response.data;
  }

  /**
   * Create a new order
   */
  static async createOrder(order: CreateOrderRequest): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.post<{ order: ApiOrderDetailsResponse }>(`${this.BASE_PATH}`, order);
    return mapOrderWrapper(response.data);
  }

  /**
   * Update an existing order
   */
  static async updateOrder(id: number, order: UpdateOrderRequest): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.put<{ order: ApiOrderDetailsResponse }>(`${this.BASE_PATH}/${id}`, order);
    return mapOrderWrapper(response.data);
  }

  /**
   * Cancel an order
   */
  static async cancelOrder(id: number, reason?: string): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.post<{ order: ApiOrderDetailsResponse }>(`${this.BASE_PATH}/${id}/cancel`, {
      reason,
    });
    return mapOrderWrapper(response.data);
  }

  /**
   * Update order status (admin only)
   */
  static async updateOrderStatus(
    id: number,
    statusUpdate: UpdateOrderStatusRequest
  ): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.post<{ order: ApiOrderDetailsResponse }>(`${this.BASE_PATH}/${id}/status`, statusUpdate);
    return mapOrderWrapper(response.data);
  }

  /**
   * Add tracking number to an order (admin only)
   */
  static async addTrackingNumber(
    id: number,
    tracking: AddTrackingNumberRequest
  ): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.post<{ order: ApiOrderDetailsResponse }>(`${this.BASE_PATH}/${id}/tracking`, tracking);
    return mapOrderWrapper(response.data);
  }

  /**
   * Helper method to check if user has admin permissions
   */
  static isAdmin(): boolean {
    return canViewAllOrders(getStoredGrantedScopes());
  }
}

// Export convenience methods
export const ordersApi = OrdersApi;
