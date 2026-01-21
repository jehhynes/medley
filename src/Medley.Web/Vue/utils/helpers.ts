import type { ArticleDto, ArticleTypeDto, ArticleStatus } from '@/types/api-client';

let articleTypes: ArticleTypeDto[] = [];
let articleTypesLoaded = false;
let articleTypesPromise: Promise<ArticleTypeDto[]> | null = null;

export const getArticleTypes = async (): Promise<ArticleTypeDto[]> => {
  if (articleTypesLoaded) {
    return articleTypes;
  }

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

export interface RelativeTimeOptions {
  short?: boolean;
  includeTime?: boolean;
}

export const formatDate = (dateString: string | null | undefined): string => {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleDateString();
};

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

  if (diffMinutes < 1) {
    return 'Just now';
  }

  if (diffMinutes < 60) {
    if (short) {
      return `${diffMinutes}m ago`;
    }
    return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`;
  }

  if (diffHours < 24) {
    if (short) {
      return `${diffHours}h ago`;
    }
    return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
  }

  if (diffDays < 7) {
    if (short) {
      return `${diffDays}d ago`;
    }
    return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
  }

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

export const getStatusBadgeClass = (status: ArticleStatus | string | null | undefined): string => {
  const classes: Record<string, string> = {
    'Draft': 'bg-secondary',
    'Review': 'bg-info',
    'Approved': 'bg-success',
    'Archived': 'bg-warning'
  };
  return classes[status as string] || 'bg-secondary';
};

export const copyToClipboard = async (text: string): Promise<boolean> => {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch (err) {
    console.error('Failed to copy:', err);
    return false;
  }
};

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

export const getIconClass = (icon: string | null | undefined, fallback: string = 'bi-file-text'): string => {
  if (!icon) {
      icon = fallback;
  }
  if (icon.startsWith('bi-')) {
    return `bi ${icon}`;
  }
  if (icon.startsWith('fa-')) {
    return `fas ${icon}`;
  }
  return icon;
};

const normalizeText = (str: string | null | undefined): string => {
  if (!str) return '';
  return str.replace(/[^a-zA-Z]/g, '').toLowerCase();
};

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

export const getStatusIcon = (status: ArticleStatus | string | null | undefined): string => {
  const iconMap: Record<string, string> = {
    'Draft': 'fa fa-pen-circle',
    'Review': 'bi-person-circle',
    'Approved': 'bi-check-circle',
    'Archived': 'bi-archive'
  };
  return iconMap[status as string] || 'bi-circle';
};

export const getStatusColorClass = (status: ArticleStatus | string | null | undefined): string => {
  const colorMap: Record<string, string> = {
    'Draft': 'text-secondary',
    'Review': 'text-info',
    'Approved': 'text-success',
    'Archived': 'text-danger'
  };
  return colorMap[status as string] || 'text-secondary';
};

export const initializeMarkdownRenderer = (): any | null => {
  if (typeof (window as any).marked !== 'undefined') {
    return (window as any).marked;
  }
  console.error('Marked.js library not loaded');
  return null;
};

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

export type ToastType = 'success' | 'error' | 'warning' | 'info';

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

export interface TreeItem {
  id: string;
  [key: string]: any;
}

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

export const findInList = <T extends TreeItem>(
  items: T[] | null | undefined,
  id: string
): T | null => {
  if (!Array.isArray(items)) return null;
  return items.find(i => i && i.id === id) || null;
};

export const showProcessingSpinner = (article: ArticleSummaryDto | null | undefined): boolean => {
  if (!article) return false;
  return article.currentConversation?.isRunning === true;
};

export const showUserTurnIndicator = (article: ArticleSummaryDto | null | undefined): boolean => {
  if (!article || !article.currentConversation) {
    return false;
  }

  if (article.currentConversation.state !== 'Active' || article.currentConversation.isRunning === true) {
    return false;
  }

  const status = article.status?.toLowerCase();
  return status !== 'approved' && status !== 'archived';
};
