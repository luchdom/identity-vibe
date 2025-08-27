import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { 
  AlertTriangle, 
  Home, 
  ArrowLeft, 
  RefreshCw,
  Server,
  Wifi,
  ShieldAlert
} from 'lucide-react';

interface ErrorPageProps {
  errorType?: '404' | '500' | '403' | 'network' | 'generic';
  title?: string;
  description?: string;
  showRetry?: boolean;
}

const ErrorPage: React.FC<ErrorPageProps> = ({ 
  errorType = 'generic',
  title,
  description,
  showRetry = false
}) => {
  const navigate = useNavigate();

  const handleRetry = () => {
    window.location.reload();
  };

  const handleGoBack = () => {
    navigate(-1);
  };

  const errorConfig = {
    '404': {
      icon: AlertTriangle,
      title: title || 'Page Not Found',
      description: description || 'The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.',
      color: 'text-yellow-500',
      bgColor: 'bg-yellow-50',
    },
    '500': {
      icon: Server,
      title: title || 'Internal Server Error',
      description: description || 'We encountered an unexpected error. Our team has been notified and is working on a fix.',
      color: 'text-red-500',
      bgColor: 'bg-red-50',
    },
    '403': {
      icon: ShieldAlert,
      title: title || 'Access Forbidden',
      description: description || 'You do not have permission to access this resource. Please contact your administrator if you believe this is an error.',
      color: 'text-orange-500',
      bgColor: 'bg-orange-50',
    },
    'network': {
      icon: Wifi,
      title: title || 'Connection Problem',
      description: description || 'Unable to connect to the server. Please check your internet connection and try again.',
      color: 'text-blue-500',
      bgColor: 'bg-blue-50',
    },
    'generic': {
      icon: AlertTriangle,
      title: title || 'Something went wrong',
      description: description || 'An unexpected error occurred. Please try again later.',
      color: 'text-gray-500',
      bgColor: 'bg-gray-50',
    },
  };

  const config = errorConfig[errorType];
  const IconComponent = config.icon;

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-background to-muted p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center space-y-4">
          <div className={`mx-auto w-16 h-16 rounded-full ${config.bgColor} flex items-center justify-center`}>
            <IconComponent className={`w-8 h-8 ${config.color}`} />
          </div>
          <CardTitle className="text-2xl font-bold">
            {config.title}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6 text-center">
          <p className="text-muted-foreground">
            {config.description}
          </p>
          
          <div className="flex flex-col gap-3">
            {showRetry && (
              <Button 
                onClick={handleRetry}
                className="w-full"
                variant="default"
              >
                <RefreshCw className="mr-2 h-4 w-4" />
                Try Again
              </Button>
            )}
            
            <Button 
              onClick={handleGoBack}
              variant="outline"
              className="w-full"
            >
              <ArrowLeft className="mr-2 h-4 w-4" />
              Go Back
            </Button>
            
            <Link to="/dashboard">
              <Button variant="ghost" className="w-full">
                <Home className="mr-2 h-4 w-4" />
                Go to Dashboard
              </Button>
            </Link>
          </div>
          
          {errorType === '500' && (
            <div className="mt-6 p-4 bg-muted rounded-lg">
              <p className="text-sm text-muted-foreground">
                <strong>Error ID:</strong> {Math.random().toString(36).substr(2, 9)}
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                Please reference this ID when contacting support.
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

// Specific error page components
export const NotFoundPage = () => (
  <ErrorPage errorType="404" />
);

export const ServerErrorPage = () => (
  <ErrorPage errorType="500" showRetry />
);

export const ForbiddenPage = () => (
  <ErrorPage errorType="403" />
);

export const NetworkErrorPage = () => (
  <ErrorPage errorType="network" showRetry />
);

export default ErrorPage;