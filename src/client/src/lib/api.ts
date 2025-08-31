import axios from 'axios';
import { showErrorToast } from './errorHandler';

const BASE_URL = 'http://localhost:5002';

// Correlation ID utilities
const generateCorrelationId = (): string => {
  return crypto.randomUUID();
};

const getOrCreateCorrelationId = (): string => {
  let correlationId = sessionStorage.getItem('correlationId');
  if (!correlationId) {
    correlationId = generateCorrelationId();
    sessionStorage.setItem('correlationId', correlationId);
  }
  return correlationId;
};

export const correlationId = {
  generate: generateCorrelationId,
  getCurrent: getOrCreateCorrelationId,
  clear: () => sessionStorage.removeItem('correlationId')
};

export const api = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 seconds timeout
});

// Request interceptor to add access token and correlation ID
api.interceptors.request.use(
  (config) => {
    // Add authorization token
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    // Add correlation ID
    const currentCorrelationId = getOrCreateCorrelationId();
    config.headers['X-Correlation-ID'] = currentCorrelationId;
    
    // Log API requests in development
    if (import.meta.env.DEV) {
      console.log(`[API Request] ${config.method?.toUpperCase()} ${config.url} | CorrelationId: ${currentCorrelationId}`);
    }
    
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token refresh and errors
api.interceptors.response.use(
  (response) => {
    // Log successful responses in development
    if (import.meta.env.DEV) {
      const correlationId = response.headers['x-correlation-id'] || response.config.headers['X-Correlation-ID'];
      console.log(`[API Response] ${response.status} ${response.config.method?.toUpperCase()} ${response.config.url} | CorrelationId: ${correlationId}`);
    }
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    // Handle 401 errors and attempt token refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        if (refreshToken) {
          const response = await axios.post(
            `${BASE_URL}/auth/refresh`,
            { refreshToken },
            { withCredentials: true }
          );

          const { accessToken, refreshToken: newRefreshToken } = response.data;
          
          localStorage.setItem('accessToken', accessToken);
          if (newRefreshToken) {
            localStorage.setItem('refreshToken', newRefreshToken);
          }

          // Retry the original request
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return api(originalRequest);
        }
      } catch (refreshError) {
        // Refresh failed, redirect to login
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    // Log error responses in development
    if (import.meta.env.DEV) {
      const correlationId = error.response?.headers['x-correlation-id'] || originalRequest.headers['X-Correlation-ID'];
      console.error(`[API Error] ${error.response?.status} ${originalRequest.method?.toUpperCase()} ${originalRequest.url} | CorrelationId: ${correlationId}`, error);
    }

    // Handle other errors and show toast if needed
    // Don't show toast for certain error types that should be handled by the UI
    const silentErrors = ['LOGIN_ERROR', 'REGISTRATION_ERROR'];
    if (!originalRequest.skipErrorToast && !silentErrors.includes(originalRequest.errorContext)) {
      showErrorToast(error);
    }

    return Promise.reject(error);
  }
);

// Helper function for making requests without error toasts
export const apiSilent = {
  ...api,
  request: (config: any) => api.request({ ...config, skipErrorToast: true }),
  get: (url: string, config?: any) => api.get(url, { ...config, skipErrorToast: true }),
  post: (url: string, data?: any, config?: any) => api.post(url, data, { ...config, skipErrorToast: true }),
  put: (url: string, data?: any, config?: any) => api.put(url, data, { ...config, skipErrorToast: true }),
  delete: (url: string, config?: any) => api.delete(url, { ...config, skipErrorToast: true }),
};