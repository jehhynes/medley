/**
 * URL and navigation utilities for Medley Vue pages
 * TypeScript version with full type safety
 */

export const getUrlParam = (name: string): string | null => {
  const params = new URLSearchParams(window.location.search);
  return params.get(name);
};

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

export const setupPopStateHandler = (handler: () => void): (() => void) => {
  if (typeof handler !== 'function') {
    console.warn('setupPopStateHandler: handler must be a function');
    return () => {};
  }
  window.addEventListener('popstate', handler);
  return () => window.removeEventListener('popstate', handler);
};
