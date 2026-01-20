import type { ArticleSummaryDto } from '@/types/api-client';
import { 
  getIconClass, 
  getStatusIcon, 
  getStatusColorClass, 
  showProcessingSpinner, 
  showUserTurnIndicator 
} from '@/utils/helpers';
import { useArticleTypes } from './useArticleTypes';

interface ArticleViewProps {
  // Props interface kept for compatibility but no longer requires articleTypeIconMap
}

interface ArticleViewEmits {
  (e: 'select', article: ArticleSummaryDto): void;
  (e: 'edit-article', article: ArticleSummaryDto): void;
  (e: 'create-child', parentArticleId: string): void;
}

/** Shared article view logic for ArticleTree, ArticleList, and MyWorkList */
export function useArticleView(props: ArticleViewProps, emit: ArticleViewEmits) {
  const { typeIconMap } = useArticleTypes();

  const selectArticle = (article: ArticleSummaryDto) => {
    emit('select', article);
    (window as any).MedleySidebar?.collapseLeftSidebar();
  };

  const getArticleIcon = (article: ArticleSummaryDto) => {
    return article.articleTypeId && typeIconMap.value[article.articleTypeId]
      ? typeIconMap.value[article.articleTypeId]
      : 'bi-file-text';
  };

  const editArticle = (article: ArticleSummaryDto) => emit('edit-article', article);
  const createChild = (parentArticleId: string) => emit('create-child', parentArticleId);

  return {
    selectArticle,
    getArticleIcon,
    editArticle,
    createChild,
    getIconClass,
    getStatusIcon,
    getStatusColorClass,
    showProcessingSpinner,
    showUserTurnIndicator
  };
}
