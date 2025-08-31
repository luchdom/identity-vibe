import { AxiosError } from 'axios';
import { toast } from 'sonner';

export interface ApiError {
  message: string;
  status?: number;
  code?: string;
  details?: any;
  correlationId?: string;
}

export const handleApiError = (error: unknown): ApiError => {
  if (error instanceof AxiosError) {
    const status = error.response?.status;
    const data = error.response?.data;
    const correlationId = error.response?.headers['x-correlation-id'] || error.config?.headers['X-Correlation-ID'];
    
    switch (status) {
      case 400:
        return {
          message: data?.message || 'Bad request. Please check your input.',
          status,
          code: 'BAD_REQUEST',
          details: data,
          correlationId,
        };
      case 401:
        return {
          message: 'Authentication failed. Please sign in again.',
          status,
          code: 'UNAUTHORIZED',
          details: data,
          correlationId,
        };
      case 403:
        return {
          message: 'You do not have permission to perform this action.',
          status,
          code: 'FORBIDDEN',
          details: data,
          correlationId,
        };
      case 404:
        return {
          message: 'The requested resource was not found.',
          status,
          code: 'NOT_FOUND',
          details: data,
          correlationId,
        };
      case 429:
        return {
          message: 'Too many requests. Please try again later.',
          status,
          code: 'RATE_LIMITED',
          details: data,
          correlationId,
        };
      case 500:
        return {
          message: 'Internal server error. Please try again later.',
          status,
          code: 'INTERNAL_SERVER_ERROR',
          details: data,
          correlationId,
        };
      case 502:
      case 503:
      case 504:
        return {
          message: 'Service temporarily unavailable. Please try again later.',
          status,
          code: 'SERVICE_UNAVAILABLE',
          details: data,
          correlationId,
        };
      default:
        return {
          message: data?.message || error.message || 'An unexpected error occurred.',
          status,
          code: 'API_ERROR',
          details: data,
          correlationId,
        };
    }
  }
  
  if (error instanceof Error) {
    return {
      message: error.message,
      code: 'GENERIC_ERROR',
    };
  }
  
  return {
    message: 'An unknown error occurred.',
    code: 'UNKNOWN_ERROR',
  };
};

export const showErrorToast = (error: unknown) => {
  const apiError = handleApiError(error);
  
  const getDescription = (message: string, correlationId?: string) => {
    if (correlationId && import.meta.env.DEV) {
      return `${message}\n\nCorrelation ID: ${correlationId}`;
    }
    return message;
  };
  
  switch (apiError.status) {
    case 401:
      toast.error('Authentication Error', {
        description: getDescription(apiError.message, apiError.correlationId),
      });
      break;
    case 403:
      toast.error('Access Denied', {
        description: getDescription(apiError.message, apiError.correlationId),
      });
      break;
    case 404:
      toast.error('Not Found', {
        description: getDescription(apiError.message, apiError.correlationId),
      });
      break;
    case 500:
      toast.error('Server Error', {
        description: getDescription(apiError.message, apiError.correlationId),
      });
      break;
    default:
      toast.error('Error', {
        description: getDescription(apiError.message, apiError.correlationId),
      });
  }
};

export const shouldShowErrorPage = (error: ApiError): boolean => {
  return error.status === 500 || error.status === 502 || error.status === 503 || error.status === 504;
};

export const getErrorPageType = (error: ApiError): '404' | '500' | '403' | 'network' | 'generic' => {
  switch (error.status) {
    case 404:
      return '404';
    case 403:
      return '403';
    case 500:
    case 502:
    case 503:
    case 504:
      return '500';
    case undefined:
      return 'network';
    default:
      return 'generic';
  }
};