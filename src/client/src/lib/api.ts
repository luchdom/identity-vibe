import axios from 'axios';
import { showErrorToast } from './errorHandler';

const BASE_URL = 'http://localhost:5002';

export const api = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 seconds timeout
});

// Request interceptor to add access token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token refresh and errors
api.interceptors.response.use(
  (response) => response,
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