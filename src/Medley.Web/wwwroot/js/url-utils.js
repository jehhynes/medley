// Shared URL and navigation utilities for Medley Vue pages
(function () {
    const getUrlParam = (name) => {
        const params = new URLSearchParams(window.location.search);
        return params.get(name);
    };

    const setUrlParam = (name, value, replaceState = false) => {
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

    const setupPopStateHandler = (handler) => {
        if (typeof handler !== 'function') return;
        window.addEventListener('popstate', handler);
        return () => window.removeEventListener('popstate', handler);
    };

    window.UrlUtils = {
        getUrlParam,
        setUrlParam,
        setupPopStateHandler
    };
})();

