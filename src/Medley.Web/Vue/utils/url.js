/**
 * URL and navigation utilities for Medley Vue pages
 * ES6 module version
 */

/**
 * Get URL parameter value
 * @param {string} name - Parameter name
 * @returns {string|null} Parameter value or null
 */
export const getUrlParam = (name) => {
    const params = new URLSearchParams(window.location.search);
    return params.get(name);
};

/**
 * Set URL parameter value
 * @param {string} name - Parameter name
 * @param {string|null} value - Parameter value (null to remove)
 * @param {boolean} replaceState - Use replaceState instead of pushState
 */
export const setUrlParam = (name, value, replaceState = false) => {
    const url = new URL(window.location);
    if (value === null || value === undefined || value === '') {
        url.searchParams.delete(name);
    } else {
        url.searchParams.set(name, value);
    }
    if (replaceState) {
        window.history.replaceState({}, '', url);
    } else {
        window.history.pushState({}, '', url);
    }
};

/**
 * Setup popstate event handler for browser back/forward navigation
 * @param {Function} handler - Handler function to call on popstate
 * @returns {Function} Cleanup function to remove the handler
 */
export const setupPopStateHandler = (handler) => {
    if (typeof handler !== 'function') return;
    window.addEventListener('popstate', handler);
    return () => window.removeEventListener('popstate', handler);
};
