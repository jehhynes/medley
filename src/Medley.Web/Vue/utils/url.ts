/**
 * URL and navigation utilities for Medley Vue pages
 * TypeScript version with full type safety
 */

/**
 * Get URL parameter value from current window location
 * @param name - Parameter name
 * @returns Parameter value or null if not found
 * 
 * @example
 * ```typescript
 * // URL: https://example.com/page?id=123&name=test
 * const id = getUrlParam('id'); // "123"
 * const name = getUrlParam('name'); // "test"
 * const missing = getUrlParam('missing'); // null
 * ```
 */
export const getUrlParam = (name: string): string | null => {
  const params = new URLSearchParams(window.location.search);
  return params.get(name);
};

/**
 * Set URL parameter value in current window location
 * Updates the browser URL without reloading the page
 * 
 * @param name - Parameter name
 * @param value - Parameter value (null to remove the parameter)
 * @param replaceState - Use replaceState instead of pushState (default: false)
 * 
 * @example
 * ```typescript
 * // Add or update parameter
 * setUrlParam('id', '123'); // Adds to history
 * setUrlParam('id', '456', true); // Replaces current history entry
 * 
 * // Remove parameter
 * setUrlParam('id', null);
 * ```
 */
export const setUrlParam = (
  name: string,
  value: string | null | undefined,
  replaceState: boolean = false
): void => {
  const url = new URL(window.location.href);
  if (value === null || value === undefined || value === '') {
    url.searchParams.delete(name);
  } else {
    url.searchParams.set(name, value);
  }
  if (replaceState) {
    window.history.replaceState({}, '', url.toString());
  } else {
    window.history.pushState({}, '', url.toString());
  }
};

/**
 * Setup popstate event handler for browser back/forward navigation
 * Useful for handling browser navigation events in single-page applications
 * 
 * @param handler - Handler function to call on popstate event
 * @returns Cleanup function to remove the event handler
 * 
 * @example
 * ```typescript
 * // In a Vue component
 * onMounted(() => {
 *   const cleanup = setupPopStateHandler(() => {
 *     console.log('User navigated back/forward');
 *     // Handle navigation
 *   });
 *   
 *   onUnmounted(() => {
 *     cleanup(); // Remove handler when component unmounts
 *   });
 * });
 * ```
 */
export const setupPopStateHandler = (handler: () => void): (() => void) => {
  if (typeof handler !== 'function') {
    console.warn('setupPopStateHandler: handler must be a function');
    return () => {}; // Return no-op cleanup function
  }
  window.addEventListener('popstate', handler);
  return () => window.removeEventListener('popstate', handler);
};

/**
 * Get all URL parameters as an object
 * @returns Object with all URL parameters
 * 
 * @example
 * ```typescript
 * // URL: https://example.com/page?id=123&name=test&active=true
 * const params = getAllUrlParams();
 * // { id: "123", name: "test", active: "true" }
 * ```
 */
export const getAllUrlParams = (): Record<string, string> => {
  const params = new URLSearchParams(window.location.search);
  const result: Record<string, string> = {};
  params.forEach((value, key) => {
    result[key] = value;
  });
  return result;
};

/**
 * Set multiple URL parameters at once
 * @param params - Object with parameter names and values
 * @param replaceState - Use replaceState instead of pushState (default: false)
 * 
 * @example
 * ```typescript
 * setUrlParams({
 *   id: '123',
 *   name: 'test',
 *   active: 'true'
 * });
 * ```
 */
export const setUrlParams = (
  params: Record<string, string | null | undefined>,
  replaceState: boolean = false
): void => {
  const url = new URL(window.location.href);
  Object.entries(params).forEach(([key, value]) => {
    if (value === null || value === undefined || value === '') {
      url.searchParams.delete(key);
    } else {
      url.searchParams.set(key, value);
    }
  });
  if (replaceState) {
    window.history.replaceState({}, '', url.toString());
  } else {
    window.history.pushState({}, '', url.toString());
  }
};

/**
 * Clear all URL parameters
 * @param replaceState - Use replaceState instead of pushState (default: true)
 * 
 * @example
 * ```typescript
 * clearUrlParams(); // Removes all query parameters
 * ```
 */
export const clearUrlParams = (replaceState: boolean = true): void => {
  const url = new URL(window.location.href);
  url.search = '';
  if (replaceState) {
    window.history.replaceState({}, '', url.toString());
  } else {
    window.history.pushState({}, '', url.toString());
  }
};

/**
 * Check if a URL parameter exists
 * @param name - Parameter name
 * @returns True if parameter exists, false otherwise
 * 
 * @example
 * ```typescript
 * if (hasUrlParam('id')) {
 *   const id = getUrlParam('id');
 * }
 * ```
 */
export const hasUrlParam = (name: string): boolean => {
  const params = new URLSearchParams(window.location.search);
  return params.has(name);
};
