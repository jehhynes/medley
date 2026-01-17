/**
 * Shared utility functions for Medley Vue apps
 * TypeScript version with full type safety
 */

import type { ArticleDto, ArticleTypeDto, ArticleStatus } from '@/types/generated/api-client';

// Global article types cache
let articleTypes: ArticleTypeDto[] = [];
let articleTypesLoaded = false;
let articleTypesPromise: Promise<ArticleTypeDto[]> | null = null;

/**
 * Get article types (loads from API on first call and caches)
 * @returns Promise resolving to article types array
 */
export const getArticleTypes = async (): Promise<ArticleTypeDto[]> => {
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
 * Options for formatting relative time
 */
export interface RelativeTimeOptions {
  /** Use short format (e.g., "2m ago" vs "2 minutes ago") */
  short?: boolean;
  /** Include time in fallback date format for old dates */
  includeTime?: boolean;
}

/**
 * Formats a date string into a readable date (no time)
 * @param dateString - ISO date string
 * @returns Formatted date string
 */
export const formatDate = (dateString: string | null | undefined): string => {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleDateString();
};

/**
 * Formats a date as relative time (e.g., "2 minutes ago", "3h ago")
 * @param dateInput - Date string or Date object
 * @param options - Formatting options
 * @returns Relative time string
 */
export const formatRelativeTime = (
  dateInput: string | Date | null | undefined,
  options: RelativeTimeOptions = {}
): string => {
  const { short = false, includeTime = false } = options;

  if (!dateInput) return 'N/A';

  const date = dateInput instanceof Date ? dateInput : new Date(dateInput);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
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
 * @param status - Article status
 * @returns Bootstrap badge class
 */
export const getStatusBadgeClass = (status: ArticleStatus | string | null | undefined): string => {
  const classes: Record<string, string> = {
    'Draft': 'bg-secondary',
    'Review': 'bg-info',
    'Approved': 'bg-success',
    'Archived': 'bg-warning'
  };
  return classes[status as string] || 'bg-secondary';
};

/**
 * Copies text to clipboard
 * @param text - Text to copy
 * @returns Promise resolving to true if successful
 */
export const copyToClipboard = async (text: string): Promise<boolean> => {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch (err) {
    console.error('Failed to copy:', err);
    return false;
  }
};

/**
 * Debounce helper - delays function execution until after wait time has elapsed
 * @param func - Function to debounce
 * @param wait - Wait time in milliseconds
 * @returns Debounced function
 */
export const debounce = <T extends (...args: any[]) => any>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: ReturnType<typeof setTimeout> | undefined;
  return function executedFunction(...args: Parameters<T>) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
};

/**
 * Get icon class from icon name
 * @param icon - Icon name
 * @returns Full icon class
 */
export const getIconClass = (icon: string | null | undefined): string => {
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
 * Normalize text for comparison (removes non-alphabetic characters and converts to lowercase)
 * @param str - String to normalize
 * @returns Normalized string
 */
const normalizeText = (str: string | null | undefined): string => {
  if (!str) return '';
  return str.replace(/[^a-zA-Z]/g, '').toLowerCase();
};

/**
 * Get icon for fragment category
 * @param category - Fragment category
 * @returns Icon class
 */
export const getFragmentCategoryIcon = (category: string | null | undefined): string => {
  if (!category) {
    return 'bi-file-text';
  }

  const normalizedCategory = normalizeText(category);

  const hardcodedIcons: Record<string, string> = {
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
 * @param type - Source type
 * @returns Icon class
 */
export const getSourceTypeIcon = (type: string | null | undefined): string => {
  const icons: Record<string, string> = {
    'Meeting': 'bi-camera-video',
    'Document': 'bi-file-text',
    'Email': 'bi-envelope',
    'Chat': 'bi-chat-dots',
    'Repository': 'bi-git',
    'Other': 'bi-file-earmark'
  };
  return icons[type || ''] || 'bi-file-earmark';
};

/**
 * Get icon for confidence level
 * @param confidence - Confidence level
 * @returns Icon class
 */
export const getConfidenceIcon = (confidence: string | null | undefined): string => {
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
 * @param confidence - Confidence level
 * @returns CSS color value
 */
export const getConfidenceColor = (confidence: string | null | undefined): string => {
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
 * @param status - Article status
 * @returns Icon class
 */
export const getStatusIcon = (status: ArticleStatus | string | null | undefined): string => {
  const iconMap: Record<string, string> = {
    'Draft': 'fa fa-pen-circle',
    'Review': 'bi-person-circle',
    'Approved': 'bi-check-circle',
    'Archived': 'bi-archive'
  };
  return iconMap[status as string] || 'bi-circle';
};

/**
 * Get color class for article status
 * @param status - Article status
 * @returns Bootstrap color class
 */
export const getStatusColorClass = (status: ArticleStatus | string | null | undefined): string => {
  const colorMap: Record<string, string> = {
    'Draft': 'text-secondary',
    'Review': 'text-info',
    'Approved': 'text-success',
    'Archived': 'text-danger'
  };
  return colorMap[status as string] || 'text-secondary';
};

/**
 * Initialize markdown renderer
 * @returns Marked instance or null if not available
 */
export const initializeMarkdownRenderer = (): any | null => {
  if (typeof (window as any).marked !== 'undefined') {
    return (window as any).marked;
  }
  console.error('Marked.js library not loaded');
  return null;
};

/**
 * Ensure toast container exists in DOM
 * @returns Toast container element
 */
const ensureToastContainer = (): HTMLElement => {
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
 * Toast notification types
 */
export type ToastType = 'success' | 'error' | 'warning' | 'info';

/**
 * Show toast notification
 * @param type - Toast type (success, error, warning, info)
 * @param message - Toast message
 */
export const showToast = (type: ToastType, message: string): void => {
  ensureToastContainer();

  const toastEl = document.createElement('div');
  const bgClass = type === 'error' ? 'danger' : type;
  toastEl.className = `toast align-items-center text-bg-${bgClass}`;
  toastEl.setAttribute('role', 'alert');
  toastEl.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>
  `;

  const container = document.getElementById('toast-container');
  if (container) {
    container.appendChild(toastEl);
    const toast = new (window as any).bootstrap.Toast(toastEl, { autohide: true, delay: 5000 });
    toast.show();

    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
  }
};

/**
 * Generic tree item interface
 */
export interface TreeItem {
  id: string;
  [key: string]: any;
}

/**
 * Recursively finds an item in a tree structure by ID
 * @param items - Array of items to search
 * @param id - ID to search for
 * @param childrenKey - Key name for children array (default: 'children')
 * @returns Found item or null
 */
export const findInTree = <T extends TreeItem>(
  items: T[] | null | undefined,
  id: string,
  childrenKey: string = 'children'
): T | null => {
  if (!Array.isArray(items)) return null;
  for (const item of items) {
    if (item.id === id) return item;
    const children = item[childrenKey] as T[] | undefined;
    if (children && children.length) {
      const found = findInTree(children, id, childrenKey);
      if (found) return found;
    }
  }
  return null;
};

/**
 * Finds an item in a flat list by ID
 * @param items - Array of items to search
 * @param id - ID to search for
 * @returns Found item or null
 */
export const findInList = <T extends TreeItem>(
  items: T[] | null | undefined,
  id: string
): T | null => {
  if (!Array.isArray(items)) return null;
  return items.find(i => i && i.id === id) || null;
};

/**
 * Check if article should show processing spinner
 * Priority: Always show if conversation is processing
 * @param article - Article to check
 * @returns True if should show spinner
 */
export const showProcessingSpinner = (article: ArticleDto | null | undefined): boolean => {
  if (!article) return false;
  return article.currentConversation?.isRunning === true;
};

/**
 * Check if article should show user turn indicator
 * Only show if conversation is active (not processing) and article status is not Approved or Archived
 * @param article - Article to check
 * @returns True if should show user turn indicator
 */
export const showUserTurnIndicator = (article: ArticleDto | null | undefined): boolean => {
  if (!article || !article.currentConversation) {
    return false;
  }

  if (article.currentConversation.state !== 'Active' || article.currentConversation.isRunning === true) {
    return false;
  }

  // Only show if status is not Approved or Archived
  const status = article.status?.toLowerCase();
  return status !== 'approved' && status !== 'archived';
};
