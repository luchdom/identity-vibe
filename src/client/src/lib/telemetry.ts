import { WebSDK } from '@opentelemetry/sdk-web'
import { ConsoleSpanExporter, BatchSpanProcessor } from '@opentelemetry/sdk-web'
import { OTLPTraceExporter } from '@opentelemetry/exporter-otlp-http'
import { Resource } from '@opentelemetry/resources'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load'
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction'
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch'
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request'
import { trace } from '@opentelemetry/api'

// Initialize OpenTelemetry for the frontend with automatic instrumentation
export function initializeTelemetry() {
  // Create resource attributes
  const resource = new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: 'identity-frontend',
    [SemanticResourceAttributes.SERVICE_VERSION]: '1.0.0',
    [SemanticResourceAttributes.SERVICE_NAMESPACE]: 'identity-system',
    [SemanticResourceAttributes.DEPLOYMENT_ENVIRONMENT]: import.meta.env.MODE || 'development',
  })

  // Configure OTLP exporter (HTTP endpoint for web applications)
  const otlpExporter = new OTLPTraceExporter({
    url: import.meta.env.VITE_OTEL_ENDPOINT || 'http://localhost:4318/v1/traces',
    headers: {},
  })

  // Create SDK instance
  const sdk = new WebSDK({
    resource,
    spanProcessor: new BatchSpanProcessor(otlpExporter),
    instrumentations: [
      // Automatic instrumentations
      new DocumentLoadInstrumentation(),
      new UserInteractionInstrumentation({
        eventNames: ['click', 'submit', 'keydown', 'change']
      }),
      new FetchInstrumentation({
        // Automatically trace API calls
        propagateTraceHeaderCorsUrls: [
          /localhost:5002/, // Gateway
          /localhost:5000/, // AuthServer
          /localhost:5003/, // ServiceA
        ],
        clearTimingResources: true,
        requestHook: (span, request) => {
          // Add custom attributes to fetch requests
          span.setAttributes({
            'http.request.correlation_id': generateCorrelationId(),
            'user.authenticated': isUserAuthenticated(),
          })
        },
      }),
      new XMLHttpRequestInstrumentation({
        propagateTraceHeaderCorsUrls: [
          /localhost:5002/,
          /localhost:5000/,
          /localhost:5003/,
        ],
      }),
    ],
  })

  // Add console exporter for development
  if (import.meta.env.DEV) {
    sdk.addSpanProcessor(new BatchSpanProcessor(new ConsoleSpanExporter()))
  }

  // Start the SDK
  sdk.start()

  console.log('OpenTelemetry initialized for frontend')
  
  return sdk
}

// Helper function to generate correlation ID
function generateCorrelationId(): string {
  return `frontend-${Math.random().toString(36).substr(2, 9)}-${Date.now()}`
}

// Helper function to check if user is authenticated
function isUserAuthenticated(): boolean {
  // Check localStorage or your auth state
  const token = localStorage.getItem('authToken')
  return !!token
}

// Enhanced API interceptor with automatic tracing
export function setupApiTracing() {
  // Automatically add correlation headers to all requests
  const originalFetch = window.fetch

  window.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers)
    
    // Add correlation ID
    if (!headers.has('X-Correlation-ID')) {
      headers.set('X-Correlation-ID', generateCorrelationId())
    }

    // Add user context if available
    const token = localStorage.getItem('authToken')
    if (token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    return originalFetch(input, {
      ...init,
      headers,
    })
  }
}

// Manually trace user actions (optional - for specific business events)
export function traceUserAction(actionName: string, attributes: Record<string, string | number | boolean> = {}) {
  const tracer = trace.getTracer('identity-frontend-manual')
  
  const span = tracer.startSpan(`user.action.${actionName}`)
  
  // Add default attributes
  span.setAttributes({
    'action.name': actionName,
    'action.timestamp': Date.now(),
    'user.authenticated': isUserAuthenticated(),
    ...attributes
  })

  // End span immediately for user actions
  span.end()
}

// Error tracking with OpenTelemetry
export function traceError(error: Error, context: Record<string, any> = {}) {
  const tracer = trace.getTracer('identity-frontend-errors')
  
  const span = tracer.startSpan('error.occurred')
  
  span.setAttributes({
    'error.type': error.name,
    'error.message': error.message,
    'error.stack': error.stack || '',
    'error.timestamp': Date.now(),
    'user.authenticated': isUserAuthenticated(),
    ...context
  })

  span.recordException(error)
  span.end()
}