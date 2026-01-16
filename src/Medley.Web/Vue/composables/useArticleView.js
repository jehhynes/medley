import { 
  getIconClass, 
  getStatusIcon, 
  getStatusColorClass, 
  showProcessingSpinner, 
  showUserTurnIndicator 
} from '@/utils/helpers.js';

/**
 * Composable for shared article view logic across ArticleTree, ArticleList, and MyWorkList components.
 * Eliminates code duplication by providing common methods and helpers.
 * 
 * @param {Object} props - Component props
 * @param {Function} emit - Component emit function
 * @returns {Object} Shared methods and helpers
 */
export function useArticleView(props, emit) {
  /**
   * Select an article and emit selection event.
   * Also collapses the left sidebar on mobile devices.
   * 
   * @param {Object} article - Article to select
   */
  const selectArticle = (article) => {
    emit('select', article);
    // Collapse left sidebar on mobile after selection
    window.MedleySidebar?.collapseLeftSidebar();
  };

  /**
   * Get the icon for an article based on its type.
   * Falls back to 'bi-file-text' if no icon is found.
   * 
   * @param {Object} article - Article object
   * @returns {string} Icon class name
   */
  const getArticleIcon = (article) => {
    // Look up icon from dictionary, fallback to bi-file-text
    if (article.articleTypeId && props.articleTypeIconMap[article.articleTypeId]) {
      return props.articleTypeIconMap[article.articleTypeId];
    }
    return 'bi-file-text';
  };

  /**
   * Emit edit article event.
   * 
   * @param {Object} article - Article to edit
   */
  const editArticle = (article) => {
    emit('edit-article', article);
  };

  /**
   * Emit create child article event.
   * 
   * @param {string} parentArticleId - ID of parent article
   */
  const createChild = (parentArticleId) => {
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
