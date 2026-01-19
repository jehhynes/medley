import type { ArticleDto } from '@/types/api-client';
import { 
  getIconClass, 
  getStatusIcon, 
  getStatusColorClass, 
  showProcessingSpinner, 
  showUserTurnIndicator 
} from '@/utils/helpers';

/**
 * Props interface for components using this composable
 */
interface ArticleViewProps {
  articleTypeIconMap: Record<string, string>;
}

/**
 * Emits interface for components using this composable
 */
interface ArticleViewEmits {
  (e: 'select', article: ArticleSummaryDto): void;
  (e: 'edit-article', article: ArticleSummaryDto): void;
  (e: 'create-child', parentArticleId: string): void;
}

/**
 * Return type for useArticleView composable
 */
interface UseArticleViewReturn {
  selectArticle: (article: ArticleSummaryDto) => void;
  getArticleIcon: (article: ArticleSummaryDto) => string;
  editArticle: (article: ArticleSummaryDto) => void;
  createChild: (parentArticleId: string) => void;
  getIconClass: typeof getIconClass;
  getStatusIcon: typeof getStatusIcon;
  getStatusColorClass: typeof getStatusColorClass;
  showProcessingSpinner: typeof showProcessingSpinner;
  showUserTurnIndicator: typeof showUserTurnIndicator;
}

/**
 * Composable for shared article view logic across ArticleTree, ArticleList, and MyWorkList components.
 * Eliminates code duplication by providing common methods and helpers.
 * 
 * @param props - Component props
 * @param emit - Component emit function
 * @returns Shared methods and helpers
 */
export function useArticleView(
  props: ArticleViewProps,
  emit: ArticleViewEmits
): UseArticleViewReturn {
  /**
   * Select an article and emit selection event.
   * Also collapses the left sidebar on mobile devices.
   * 
   * @param article - Article to select
   */
  const selectArticle = (article: ArticleSummaryDto): void => {
    emit('select', article);
    // Collapse left sidebar on mobile after selection
    (window as any).MedleySidebar?.collapseLeftSidebar();
  };

  /**
   * Get the icon for an article based on its type.
   * Falls back to 'bi-file-text' if no icon is found.
   * 
   * @param article - Article object
   * @returns Icon class name
   */
  const getArticleIcon = (article: ArticleSummaryDto): string => {
    // Look up icon from dictionary, fallback to bi-file-text
    if (article.articleTypeId && props.articleTypeIconMap[article.articleTypeId]) {
      return props.articleTypeIconMap[article.articleTypeId];
    }
    return 'bi-file-text';
  };

  /**
   * Emit edit article event.
   * 
   * @param article - Article to edit
   */
  const editArticle = (article: ArticleSummaryDto): void => {
    emit('edit-article', article);
  };

  /**
   * Emit create child article event.
   * 
   * @param parentArticleId - ID of parent article
   */
  const createChild = (parentArticleId: string): void => {
    emit('create-child', parentArticleId);
  };

  return {
    // Methods
    selectArticle,
    getArticleIcon,
    editArticle,
    createChild,
    
    // Helper functions (re-exported for convenience)
    getIconClass,
    getStatusIcon,
    getStatusColorClass,
    showProcessingSpinner,
    showUserTurnIndicator
  };
}
