import { ordersApi as ordersHttp } from './api';

import type {
  CreateOrderRequest,
  OrderDetails,
  OrderListResponse,
  OrdersQueryParams,
} from '@/types/orders';

export class OrdersApi {
  private static readonly BASE_PATH = '/orders';

  static async getOrders(params?: OrdersQueryParams, signal?: AbortSignal): Promise<OrderListResponse> {
    const response = await ordersHttp.get<OrderListResponse>(this.BASE_PATH, {
      params,
      signal,
    });
    return response.data;
  }

  static async getAllOrders(params?: OrdersQueryParams, signal?: AbortSignal): Promise<OrderListResponse> {
    const response = await ordersHttp.get<OrderListResponse>(`${this.BASE_PATH}/all`, {
      params,
      signal,
    });
    return response.data;
  }

  static async getOrderById(id: number, signal?: AbortSignal): Promise<OrderDetails> {
    const response = await ordersHttp.get<OrderDetails>(`${this.BASE_PATH}/${id}`, {
      signal,
    });
    return response.data;
  }

  static async createOrder(payload: CreateOrderRequest): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.post<{ order: OrderDetails }>(this.BASE_PATH, payload);
    return response.data;
  }

  static async cancelOrder(id: number, cancellationReason?: string): Promise<{ order: OrderDetails }> {
    const response = await ordersHttp.post<{ order: OrderDetails }>(`${this.BASE_PATH}/${id}/cancel`, {
      cancellationReason,
    });
    return response.data;
  }
}

export const ordersApi = OrdersApi;
