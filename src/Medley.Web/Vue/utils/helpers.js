/**
 * Shared utility functions for Medley Vue apps
 * ES6 module version
 */

// Global article types cache
let articleTypes = [];
let articleTypesLoaded = false;
let articleTypesPromise = null;

/**
 * Get article types (loads from API on first call and caches)
 * @returns {Promise<Array>} Article types array
 */
export const getArticleTypes = async () => {
    if (articleTypesLoaded) {
        return articleTypes;
    }

    // If already loading, return the existing promise
    if (articleTypesPromise) {
        return articleTypesPromise;
    }

    articleTypesPromise = (async () => {
        try {
            const response = await fetch('/api/articles/types');
            if (!response.ok) {
                throw new Error('Failed to load article types');
            }
            articleTypes = await response.json();
            articleTypesLoaded = true;
            return articleTypes;
        } catch (err) {
            console.error('Error loading article types:', err);
            articleTypes = [];
            return articleTypes;
        } finally {
            articleTypesPromise = null;
        }
    })();

    return articleTypesPromise;
};

/**
 * Formats a date string into a readable date (no time)
 * @param {string} dateString - ISO date string
 * @returns {string} Formatted date string
 */
export const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString();
};

/**
 * Formats a date as relative time (e.g., "2 minutes ago", "3h ago")
 * @param {string|Date} dateInput - Date string or Date object
 * @param {Object} options - Formatting options
 * @param {boolean} options.short - Use short format (e.g., "2m ago" vs "2 minutes ago")
 * @param {boolean} options.includeTime - Include time in fallback date format for old dates
 * @returns {string} Relative time string
 */
export const formatRelativeTime = (dateInput, options = {}) => {
    const { short = false, includeTime = false } = options;
    
    if (!dateInput) return 'N/A';
    
    const date = dateInput instanceof Date ? dateInput : new Date(dateInput);
    const now = new Date();
    const diffMs = now - date;
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    // Just now
    if (diffMinutes < 1) {
        return 'Just now';
    }
    
    // Minutes
    if (diffMinutes < 60) {
        if (short) {
            return `${diffMinutes}m ago`;
        }
        return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`;
    }
    
    // Hours
    if (diffHours < 24) {
        if (short) {
            return `${diffHours}h ago`;
        }
        return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    }
    
    // Days (within a week)
    if (diffDays < 7) {
        if (short) {
            return `${diffDays}d ago`;
        }
        return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
    }
    
    // Older dates - show formatted date
    if (includeTime) {
        return date.toLocaleDateString([], {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }
    
    return date.toLocaleDateString();
};

/**
 * Returns Bootstrap badge class for article status
 * @param {string|number} status - Article status (Draft, Review, Approved, Archived)
 * @returns {string} Bootstrap badge class
 */
export const getStatusBadgeClass = (status) => {
    const classes = {
        'Draft': 'bg-secondary',
        'Review': 'bg-info',
        'Approved': 'bg-success',
        'Archived': 'bg-warning'
    };
    return classes[status] || 'bg-secondary';
};

/**
 * Copies text to clipboard
 * @param {string} text - Text to copy
 * @returns {Promise<boolean>} True if successful
 */
export const copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy:', err);
        return false;
    }
};

/**
 * Debounce helper
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} Debounced function
 */
export const debounce = (func, wait) => {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func.apply(this, args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
};

/**
 * Get icon class from icon name
 * @param {string} icon - Icon name
 * @returns {string} Full icon class
 */
export const getIconClass = (icon) => {
    if (!icon) {
        return 'bi bi-file-text';
    }
    if (icon.startsWith('bi-')) {
        return `bi ${icon}`;
    }
    if (icon.startsWith('fa-')) {
        return `fas ${icon}`;
    }
    return 'bi bi-file-text';
};

/**
 * Normalize text for comparison
 * @param {string} str - String to normalize
 * @returns {string} Normalized string
 */
const normalizeText = (str) => {
    if (!str) return '';
    return str.replace(/[^a-zA-Z]/g, '').toLowerCase();
};

/**
 * Get icon for fragment category
 * @param {string} category - Fragment category
 * @returns {string} Icon class
 */
export const getFragmentCategoryIcon = (category) => {
    if (!category) {
        return 'bi-file-text';
    }

    const normalizedCategory = normalizeText(category);

    const hardcodedIcons = {
        'bestpractice': 'bi-shield-check'
    };

    if (hardcodedIcons[normalizedCategory]) {
        return hardcodedIcons[normalizedCategory];
    }

    // Use cached types (will be empty array if not loaded yet)
    if (Array.isArray(articleTypes) && articleTypes.length > 0) {
        const matchingType = articleTypes.find(
            at => at.name && normalizeText(at.name) === normalizedCategory
        );

        if (matchingType && matchingType.icon) {
            return matchingType.icon;
        }
    }

    return 'bi-file-text';
};

/**
 * Get icon for source type
 * @param {string} type - Source type
 * @returns {string} Icon class
 */
export const getSourceTypeIcon = (type) => {
    const icons = {
        'Meeting': 'bi-camera-video',
        'Document': 'bi-file-text',
        'Email': 'bi-envelope',
        'Chat': 'bi-chat-dots',
        'Repository': 'bi-git',
        'Other': 'bi-file-earmark'
    };
    return icons[type] || 'bi-file-earmark';
};

/**
 * Get icon for confidence level
 * @param {string} confidence - Confidence level
 * @returns {string} Icon class
 */
export const getConfidenceIcon = (confidence) => {
    if (!confidence) return 'fa-ban';
    const level = confidence.toString().toLowerCase();
    switch (level) {
        case 'certain': return 'fa-signal-bars';
        case 'high': return 'fa-signal-bars-good';
        case 'medium': return 'fa-signal-bars-fair';
        case 'low': return 'fa-signal-bars-weak';
        case 'unclear': return 'fa-ban';
        default: return 'fa-ban';
    }
};

/**
 * Get color for confidence level
 * @param {string} confidence - Confidence level
 * @returns {string} CSS color value
 */
export const getConfidenceColor = (confidence) => {
    if (!confidence) return 'var(--bs-secondary)';
    const level = confidence.toString().toLowerCase();
    switch (level) {
        case 'certain': return 'var(--bs-success)';
        case 'high': return 'var(--bs-success)';
        case 'medium': return 'var(--bs-warning)';
        case 'low': return 'var(--bs-danger)';
        case 'unclear': return 'var(--bs-danger)';
        default: return 'var(--bs-secondary)';
    }
};



/**
 * Get icon for article status
 * @param {string} status - Article status
 * @returns {string} Icon class
 */
export const getStatusIcon = (status) => {
    const iconMap = {
        'Draft': 'fa fa-pen-circle',
        'Review': 'bi-person-circle',
        'Approved': 'bi-check-circle',
        'Archived': 'bi-archive'
    };
    return iconMap[status] || 'bi-circle';
};

/**
 * Get color class for article status
 * @param {string} status - Article status
 * @returns {string} Bootstrap color class
 */
export const getStatusColorClass = (status) => {
    const colorMap = {
        'Draft': 'text-secondary',
        'Review': 'text-info',
        'Approved': 'text-success',
        'Archived': 'text-danger'
    };
    return colorMap[status] || 'text-secondary';
};

/**
 * Initialize markdown renderer
 * @returns {object|null} Marked instance or null
 */
export const initializeMarkdownRenderer = () => {
    if (typeof marked !== 'undefined') {
        return marked;
    }
    console.error('Marked.js library not loaded');
    return null;
};

/**
 * Ensure toast container exists in DOM
 * @returns {HTMLElement} Toast container element
 */
const ensureToastContainer = () => {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1090';
        document.body.appendChild(container);
    }
    return container;
};

/**
 * Show toast notification
 * @param {string} type - Toast type (success, error, warning, info)
 * @param {string} message - Toast message
 */
export const showToast = (type, message) => {
    ensureToastContainer();

    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center text-bg-${type === 'error' ? 'danger' : type}`;
    toastEl.setAttribute('role', 'alert');
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    document.getElementById('toast-container').appendChild(toastEl);
    const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 5000 });
    toast.show();

    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
};

/**
 * Recursively finds an item in a tree structure by ID
 * @param {Array} items - Array of items to search
 * @param {string} id - ID to search for
 * @param {string} childrenKey - Key name for children array (default: 'children')
 * @returns {Object|null} Found item or null
 */
export const findInTree = (items, id, childrenKey = 'children') => {
    if (!Array.isArray(items)) return null;
    for (const item of items) {
        if (item.id === id) return item;
        const children = item[childrenKey];
        if (children && children.length) {
            const found = findInTree(children, id, childrenKey);
            if (found) return found;
        }
    }
    return null;
};

/**
 * Finds an item in a flat list by ID
 * @param {Array} items - Array of items to search
 * @param {string} id - ID to search for
 * @returns {Object|null} Found item or null
 */
export const findInList = (items, id) => {
    if (!Array.isArray(items)) return null;
    return items.find(i => i && i.id === id) || null;
};

/**
 * Check if article should show processing spinner
 * Priority: Always show if conversation is processing
 * @param {Object} article - Article to check
 * @returns {boolean} True if should show spinner
 */
export const showProcessingSpinner = (article) => {
    return article.currentConversation && 
           article.currentConversation.isRunning === true;
};

/**
 * Check if article should show user turn indicator
 * Only show if conversation is active (not processing) and article status is not Approved or Archived
 * @param {Object} article - Article to check
 * @returns {boolean} True if should show user turn indicator
 */
export const showUserTurnIndicator = (article) => {
    if (!article.currentConversation || 
        article.currentConversation.state !== 'Active' ||
        article.currentConversation.isRunning === true) {
        return false;
    }
    
    // Only show if status is not Approved or Archived
    const status = article.status?.toLowerCase();
    return status !== 'approved' && status !== 'archived';
};
