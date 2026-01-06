// Shared utility functions for Medley Vue apps
(function () {
    /**
     * Formats a date string into a readable date (no time)
     * @param {string} dateString - ISO date string
     * @returns {string} Formatted date string
     */
    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleDateString();
    };

    /**
     * Returns Bootstrap badge class for article status
     * @param {string|number} status - Article status (Draft, Review, Approved, Archived)
     * @returns {string} Bootstrap badge class
     */
    const getStatusBadgeClass = (status) => {
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
     * @returns {Promise<void>}
     */
    const copyToClipboard = async (text) => {
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
     * @param {Function} func
     * @param {number} wait
     * @returns {Function}
     */
    const debounce = (func, wait) => {
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

    const getIconClass = (icon) => {
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

    const normalizeText = (str) => {
        if (!str) return '';
        return str.replace(/[^a-zA-Z]/g, '').toLowerCase();
    };

    const getFragmentCategoryIcon = (category, articleTypes) => {
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

    const getSourceTypeIcon = (type) => {
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

    const getConfidenceIcon = (confidence) => {
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

    const getConfidenceColor = (confidence) => {
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

    const getConfidenceLabel = (confidence) => {
        if (!confidence) return '';
        return confidence.toString();
    };

    const getStatusIcon = (status) => {
        const iconMap = {
            'Draft': 'fa fa-pen-circle',
            'Review': 'bi-person-circle',
            'Approved': 'bi-check-circle',
            'Archived': 'bi-archive'
        };
        return iconMap[status] || 'bi-circle';
    };

    const getStatusColorClass = (status) => {
        const colorMap = {
            'Draft': 'text-secondary',
            'Review': 'text-info',
            'Approved': 'text-success',
            'Archived': 'text-danger'
        };
        return colorMap[status] || 'text-secondary';
    };

    const initializeMarkdownRenderer = () => {
        if (typeof marked !== 'undefined') {
            return marked;
        }
        console.error('Marked.js library not loaded');
        return null;
    };

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

    const showToast = (type, message) => {
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
    const findInTree = (items, id, childrenKey = 'children') => {
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
    const findInList = (items, id) => {
        if (!Array.isArray(items)) return null;
        return items.find(i => i && i.id === id) || null;
    };

    /**
     * Check if article should show processing spinner
     * Priority: Always show if conversation is processing
     * @param {Object} article - Article to check
     * @returns {boolean} True if should show spinner
     */
    const showProcessingSpinner = (article) => {
        return article.currentConversation && 
               article.currentConversation.isRunning === true;
    };

    /**
     * Check if article should show user turn indicator
     * Only show if conversation is active (not processing) and article status is not Approved or Archived
     * @param {Object} article - Article to check
     * @returns {boolean} True if should show user turn indicator
     */
    const showUserTurnIndicator = (article) => {
        if (!article.currentConversation || 
            article.currentConversation.state !== 'Active' ||
            article.currentConversation.isRunning === true) {
            return false;
        }
        
        // Only show if status is not Approved or Archived
        const status = article.status?.toLowerCase();
        return status !== 'approved' && status !== 'archived';
    };

    // Export utilities
    window.MedleyUtils = {
        formatDate,
        getStatusBadgeClass,
        getStatusIcon,
        getStatusColorClass,
        copyToClipboard,
        debounce,
        getIconClass,
        getFragmentCategoryIcon,
        getSourceTypeIcon,
        getConfidenceIcon,
        getConfidenceColor,
        getConfidenceLabel,
        initializeMarkdownRenderer,
        showToast,
        findInTree,
        findInList,
        showProcessingSpinner,
        showUserTurnIndicator
    };
})();