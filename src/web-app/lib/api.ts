import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';

import { authEvents } from '@/lib/auth/auth-events';
import OAuth2Auth from '@/lib/auth/oauth2-auth';

const AUTH_BASE_URL = process.env.NEXT_PUBLIC_AUTH_URL || 'http://localhost:5000';
const ORDERS_BASE_URL = process.env.NEXT_PUBLIC_ORDERS_URL || 'http://localhost:5003';

type RetryableRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean;
};

function generateCorrelationId(): string {
  return crypto.randomUUID();
}

function getOrCreateCorrelationId(): string {
  if (typeof window === 'undefined') {
    return generateCorrelationId();
  }

  const existingValue = sessionStorage.getItem('correlationId');
  if (existingValue) {
    return existingValue;
  }

  const correlationId = generateCorrelationId();
  sessionStorage.setItem('correlationId', correlationId);
  return correlationId;
}

function createApiClient(baseURL: string) {
  return axios.create({
    baseURL,
    withCredentials: true,
    headers: {
      'Content-Type': 'application/json',
    },
    timeout: 10000,
  });
}

function clearStoredAuthData() {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('idToken');
  localStorage.removeItem('user');
  localStorage.removeItem('grantedScopes');
}

function attachInterceptors(client: ReturnType<typeof createApiClient>, clientName: string) {
  client.interceptors.request.use(
    (config) => {
      const token = OAuth2Auth.getAccessToken();

      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }

      config.headers['X-Correlation-ID'] = getOrCreateCorrelationId();

      if (process.env.NODE_ENV === 'development') {
        console.log(
          `[${clientName} Request] ${config.method?.toUpperCase()} ${config.url} | CorrelationId: ${String(config.headers['X-Correlation-ID'])}`
        );
      }

      return config;
    },
    (error) => Promise.reject(error)
  );

  client.interceptors.response.use(
    (response) => {
      if (process.env.NODE_ENV === 'development') {
        const correlationId = response.headers['x-correlation-id'] || response.config.headers['X-Correlation-ID'];
        console.log(
          `[${clientName} Response] ${response.status} ${response.config.method?.toUpperCase()} ${response.config.url} | CorrelationId: ${String(correlationId)}`
        );
      }

      return response;
    },
    async (error: AxiosError) => {
      if (isRequestAborted(error)) {
        return Promise.reject(error);
      }

      const originalRequest = error.config as RetryableRequestConfig | undefined;
      const originalUrl = originalRequest?.url?.toString() ?? '';

      if (
        error.response?.status === 401 &&
        originalRequest &&
        !originalRequest._retry &&
        !originalUrl.includes('/connect/token')
      ) {
        originalRequest._retry = true;

        try {
          const refreshed = await OAuth2Auth.refreshToken();
          originalRequest.headers.Authorization = `Bearer ${refreshed.accessToken}`;
          return client(originalRequest);
        } catch (refreshError) {
          clearStoredAuthData();
          authEvents.dispatchEvent(new CustomEvent('sessionExpired'));
          return Promise.reject(refreshError);
        }
      }

      if (process.env.NODE_ENV === 'development') {
        const correlationId = error.response?.headers['x-correlation-id'] || originalRequest?.headers?.['X-Correlation-ID'];
        console.error(
          `[${clientName} Error] ${error.response?.status} ${originalRequest?.method?.toUpperCase()} ${originalRequest?.url} | CorrelationId: ${String(correlationId)}`,
          error
        );
      }

      return Promise.reject(error);
    }
  );
}

export function isRequestAborted(error: unknown): boolean {
  if (axios.isCancel(error)) {
    return true;
  }

  if (typeof error === 'object' && error !== null) {
    const maybeError = error as { code?: string; name?: string; message?: string };
    return maybeError.code === 'ERR_CANCELED' || maybeError.name === 'CanceledError' || maybeError.message === 'canceled';
  }

  return false;
}

export const authApi = createApiClient(AUTH_BASE_URL);
export const ordersApi = createApiClient(ORDERS_BASE_URL);

attachInterceptors(authApi, 'Auth API');
attachInterceptors(ordersApi, 'Orders API');

export const api = ordersApi;
