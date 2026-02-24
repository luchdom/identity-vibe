import axios from 'axios';

const AUTH_BASE_URL = process.env.NEXT_PUBLIC_AUTH_URL || 'http://localhost:5000';
const ORDERS_BASE_URL = process.env.NEXT_PUBLIC_ORDERS_URL || 'http://localhost:5003';
const OAUTH_CLIENT_ID = 'web-admin';

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

// Session expired event emitter
export const authEvents = new EventTarget();

export const correlationId = {
  generate: generateCorrelationId,
  getCurrent: getOrCreateCorrelationId,
  clear: () => sessionStorage.removeItem('correlationId')
};

const createApiClient = (baseURL: string) => axios.create({
  baseURL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 seconds timeout
});

export const authApi = createApiClient(AUTH_BASE_URL);
export const ordersApi = createApiClient(ORDERS_BASE_URL);

const attachInterceptors = (client: typeof authApi, clientName: string) => {
  // Request interceptor to add access token and correlation ID
  client.interceptors.request.use(
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
    if (process.env.NODE_ENV === 'development') {
        console.log(`[${clientName} Request] ${config.method?.toUpperCase()} ${config.url} | CorrelationId: ${currentCorrelationId}`);
    }
    
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
  );

  // Response interceptor to handle token refresh and errors
  client.interceptors.response.use(
    (response) => {
    // Log successful responses in development
    if (process.env.NODE_ENV === 'development') {
      const correlationId = response.headers['x-correlation-id'] || response.config.headers['X-Correlation-ID'];
        console.log(`[${clientName} Response] ${response.status} ${response.config.method?.toUpperCase()} ${response.config.url} | CorrelationId: ${correlationId}`);
    }
    return response;
  },
  async (error) => {
    const originalRequest = error.config;
    const originalUrl = originalRequest?.url?.toString() ?? '';

    // Handle 401 errors and attempt token refresh
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !originalUrl.includes('/connect/token')) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        if (refreshToken) {
          // Use a fresh axios instance to avoid infinite recursion
          const formData = new URLSearchParams();
          formData.append('grant_type', 'refresh_token');
          formData.append('refresh_token', refreshToken);
          formData.append('client_id', OAUTH_CLIENT_ID);
          
          const response = await axios.post(
            `${AUTH_BASE_URL}/connect/token`,
            formData,
            { 
              withCredentials: true,
              headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Correlation-ID': getOrCreateCorrelationId()
              }
            }
          );

          const { access_token: accessToken, refresh_token: newRefreshToken } = response.data;
          
          localStorage.setItem('accessToken', accessToken);
          if (newRefreshToken) {
            localStorage.setItem('refreshToken', newRefreshToken);
          }

          // Retry the original request
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return client(originalRequest);
        }
      } catch (refreshError) {
        // Refresh failed, clear auth data and emit session expired event
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');

        // Emit session expired event for the AuthProvider to handle
        authEvents.dispatchEvent(new CustomEvent('sessionExpired'));

        return Promise.reject(refreshError);
      }
    }

    // Log error responses in development
    if (process.env.NODE_ENV === 'development') {
      const correlationId = error.response?.headers['x-correlation-id'] || originalRequest?.headers?.['X-Correlation-ID'];
      console.error(`[${clientName} Error] ${error.response?.status} ${originalRequest?.method?.toUpperCase()} ${originalRequest?.url} | CorrelationId: ${correlationId}`, error);
    }

    return Promise.reject(error);
  }
  );
};

attachInterceptors(authApi, 'Auth API');
attachInterceptors(ordersApi, 'Orders API');

// Temporary compatibility export for any remaining imports.
export const api = ordersApi;
