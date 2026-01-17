/**
 * Utility types and functions for validation and error handling
 */

/**
 * Nullable type helper - makes a type nullable
 */
export type Nullable<T> = T | null;

/**
 * API response wrapper for consistent response handling
 */
export type ApiResult<T> = {
  data: T;
  success: boolean;
  error?: string;
};

/**
 * Pagination request parameters
 */
export interface PaginatedRequest {
  page: number;
  pageSize: number;
}

/**
 * Paginated response wrapper
 */
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Loading state helper for async operations
 */
export interface LoadingState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

/**
 * Validation error details
 */
export interface ValidationError {
  field?: string;
  message: string;
  receivedType?: string;
  expectedType?: string;
  value?: unknown;
}

/**
 * Log a validation error with detailed information
 * This function logs validation failures for debugging purposes
 * 
 * @param context - Context where validation failed (e.g., "API response", "SignalR message")
 * @param error - Validation error details
 */
export function logValidationError(context: string, error: ValidationError): void {
  const errorMessage = [
    `[Validation Error] ${context}:`,
    `  Message: ${error.message}`,
    error.field ? `  Field: ${error.field}` : null,
    error.expectedType ? `  Expected: ${error.expectedType}` : null,
    error.receivedType ? `  Received: ${error.receivedType}` : null,
    error.value !== undefined ? `  Value: ${JSON.stringify(error.value)}` : null
  ]
    .filter(Boolean)
    .join('\n');

  console.error(errorMessage);
}

/**
 * Graceful error handler wrapper
 * Wraps a function to catch and handle validation errors gracefully
 * 
 * @param fn - Function to wrap
 * @param context - Context for error logging
 * @param fallbackValue - Value to return on error
 * @returns Wrapped function that handles errors gracefully
 */
export function withGracefulErrorHandling<TArgs extends unknown[], TReturn>(
  fn: (...args: TArgs) => TReturn,
  context: string,
  fallbackValue: TReturn
): (...args: TArgs) => TReturn {
  return (...args: TArgs): TReturn => {
    try {
      return fn(...args);
    } catch (error) {
      logValidationError(context, {
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        receivedType: typeof error,
        value: error
      });
      return fallbackValue;
    }
  };
}

/**
 * Async graceful error handler wrapper
 * Wraps an async function to catch and handle validation errors gracefully
 * 
 * @param fn - Async function to wrap
 * @param context - Context for error logging
 * @param fallbackValue - Value to return on error
 * @returns Wrapped async function that handles errors gracefully
 */
export function withGracefulErrorHandlingAsync<TArgs extends unknown[], TReturn>(
  fn: (...args: TArgs) => Promise<TReturn>,
  context: string,
  fallbackValue: TReturn
): (...args: TArgs) => Promise<TReturn> {
  return async (...args: TArgs): Promise<TReturn> => {
    try {
      return await fn(...args);
    } catch (error) {
      logValidationError(context, {
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        receivedType: typeof error,
        value: error
      });
      return fallbackValue;
    }
  };
}

/**
 * Validate and transform API response data
 * Uses a type guard to validate response data and logs errors if validation fails
 * 
 * @param data - Data to validate
 * @param typeGuard - Type guard function
 * @param context - Context for error logging
 * @returns Validated data or null if validation fails
 */
export function validateResponse<T>(
  data: unknown,
  typeGuard: (value: unknown) => value is T,
  context: string
): T | null {
  if (typeGuard(data)) {
    return data;
  }

  logValidationError(context, {
    message: 'Response data failed type validation',
    receivedType: typeof data,
    expectedType: typeGuard.name.replace('is', ''),
    value: data
  });

  return null;
}

/**
 * Create a loading state object with initial values
 * 
 * @returns Initial loading state
 */
export function createLoadingState<T>(): LoadingState<T> {
  return {
    data: null,
    loading: false,
    error: null
  };
}

/**
 * Update loading state to loading
 * 
 * @param state - Loading state to update
 */
export function setLoading<T>(state: LoadingState<T>): void {
  state.loading = true;
  state.error = null;
}

/**
 * Update loading state with success data
 * 
 * @param state - Loading state to update
 * @param data - Data to set
 */
export function setSuccess<T>(state: LoadingState<T>, data: T): void {
  state.data = data;
  state.loading = false;
  state.error = null;
}

/**
 * Update loading state with error
 * 
 * @param state - Loading state to update
 * @param error - Error message or Error object
 */
export function setError<T>(state: LoadingState<T>, error: string | Error): void {
  state.loading = false;
  state.error = error instanceof Error ? error.message : error;
}
