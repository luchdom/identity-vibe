import axios from 'axios';

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5002';

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
    if (process.env.NODE_ENV === 'development') {
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
    if (process.env.NODE_ENV === 'development') {
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
          // Use a fresh axios instance to avoid infinite recursion
          const formData = new URLSearchParams();
          formData.append('grant_type', 'refresh_token');
          formData.append('refresh_token', refreshToken);
          
          const response = await axios.post(
            `${BASE_URL}/connect/token`,
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
          return api(originalRequest);
        }
      } catch (refreshError) {
        // Refresh failed, clear auth data and redirect
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        
        // Only redirect if not already on login page
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
        return Promise.reject(refreshError);
      }
    }

    // Log error responses in development
    if (process.env.NODE_ENV === 'development') {
      const correlationId = error.response?.headers['x-correlation-id'] || originalRequest.headers['X-Correlation-ID'];
      console.error(`[API Error] ${error.response?.status} ${originalRequest.method?.toUpperCase()} ${originalRequest.url} | CorrelationId: ${correlationId}`, error);
    }

    return Promise.reject(error);
  }
);

