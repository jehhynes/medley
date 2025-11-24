// Shared utility functions for Medley Vue apps

/**
 * Formats a date string into a readable format
 * @param {string} dateString - ISO date string
 * @returns {string} Formatted date string
 */
const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
};

/**
 * Returns Bootstrap badge class for article status
 * @param {string} status - Article status (Draft, Published, Archived)
 * @returns {string} Bootstrap badge class
 */
const getStatusBadgeClass = (status) => {
    const classes = {
        'Draft': 'bg-secondary',
        'Published': 'bg-success',
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

// Export utilities
window.MedleyUtils = {
    formatDate,
    getStatusBadgeClass,
    copyToClipboard
};

